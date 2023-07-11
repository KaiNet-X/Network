namespace Net.Connection.Clients.Generic;

using Channels;
using Messages;
using Net;
using Net.Connection.Servers;
using Net.Messages.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// This is the base client that provides connection management capabilities.
/// </summary>
/// <typeparam name="MainChannel">The main channel implementing the connection protocol. It should be a reliable type, however that is not enforced.</typeparam>
public abstract class GeneralClient<MainChannel> : BaseClient where MainChannel : class, IChannel
{
    private CryptographyService _crypto = new();
    private bool _timedOut = false;
    private System.Timers.Timer _pollTimer;
    private EncryptionMessage.Stage encryptionStage = EncryptionMessage.Stage.NONE;
    private IMessageParser _parser;
    private TaskCompletionSource connectedSource = new TaskCompletionSource();

    protected SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
    protected SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    protected CancellationTokenSource TokenSource = new CancellationTokenSource();
    protected volatile ServerSettings Settings;

    /// <summary>
    /// This channel represents the connection for this server. It is best to use a reliable protocol such at TCP over one like UDP for the main connection.
    /// </summary>
    protected MainChannel Connection { get; set; }

    /// <summary>
    /// Register message handlers for custom message types with message type name
    /// </summary>
    protected readonly Dictionary<Type, Action<MessageBase>> _MessageHandlers = new()
    {
        {typeof(Object), (mb) => { } }
    };

    /// <summary>
    /// Task that completes when the connection is finished. Call this in inherrited classes to asynchrounously complete the connection.
    /// </summary>
    protected TaskCompletionSource Connected { get => connectedSource; set => connectedSource = value; }

    /// <summary>
    /// The state of the current connection.
    /// </summary>
    public ConnectionState ConnectionState { get; protected set; } = ConnectionState.NONE;

    /// <summary>
    /// Message parser the library uses (by default, NewMessageParser with MpSerializer
    /// </summary>
    public IMessageParser MessageParser { get => _parser ??= new NewMessageParser(_crypto, Consts.DefaultSerializer); init => _parser = value; }

    /// <summary>
    /// Invoked when an unregistered message is received
    /// </summary>
    public event Action<MessageBase> OnUnregisteredMessage;

    /// <summary>
    /// Invoked when disconnected from. Argument is graceful or ungraceful. 
    /// </summary>
    public event Action<DisconnectionInfo> OnDisconnect;


    private void _SendMessage(MessageBase message)
    {
        try
        {
            var bytes = MessageParser.EncapsulateMessageAsSpan(message, new Dictionary<string, string>() { { "Encryption", GetEnc() } });
            _sendSemaphore.Wait();
            Connection.SendBytes(bytes);
        }
        catch (Exception ex)
        {
            if (ConnectionState != ConnectionState.CLOSED)
                DisconnectedEvent(new DisconnectionInfo
                {
                    Exception = ex
                });
        }
        finally
        {
            _sendSemaphore.Release();
        }
    }

    /// <summary>
    /// Sends a message to the remote client.
    /// </summary>
    /// <param name="message"></param>
    public override void SendMessage(MessageBase message)
    {
        _SendMessage(message);
    }

    private async Task _SendMessageAsync(MessageBase message, CancellationToken token = default)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token, TokenSource.Token);

        try
        {
            var bytes = MessageParser.EncapsulateMessageAsMemory(message, new Dictionary<string, string>() { { "Encryption", GetEnc() } });
            await Utilities.ConcurrentAccessAsync(async (ct) =>
                await Connection.SendBytesAsync(bytes, cts.Token),
                _sendSemaphore);
        }
        catch (Exception ex)
        {
            if (ConnectionState != ConnectionState.CLOSED)
                await DisconnectedEventAsync(new DisconnectionInfo 
                {
                    Exception = ex
                });
        }
    }

    /// <summary>
    /// Sends a message to the remote client.
    /// </summary>
    /// <param name="message"></param>
    public override async Task SendMessageAsync(MessageBase message, CancellationToken token = default) =>
        await _SendMessageAsync(message, token);

    /// <summary>
    /// Generic method to register an action to handle the specified method type.
    /// </summary>
    /// <typeparam name="T">Type of the message</typeparam>
    /// <param name="handler">Action that is called whenever the message is received.</param>
    public void RegisterMessageHandler<T>(Action<T> handler) where T : MessageBase =>
        RegisterMessageHandler(mb => handler(mb as T), typeof(T));

    /// <summary>
    /// Non-generic method to register an action to handle the specified method type.
    /// </summary>
    /// <param name="handler">Action that is called whenever the message is received.</param>
    /// <param name="messageType">Type of the message to register.</param>
    public void RegisterMessageHandler(Action<MessageBase> handler, Type messageType) =>
        _MessageHandlers.Add(messageType, mb => handler(mb));

    private string GetEnc() => encryptionStage switch
    {
        EncryptionMessage.Stage.SYN => "Rsa",
        EncryptionMessage.Stage.ACK => "Aes",
        EncryptionMessage.Stage.SYNACK => "Aes",
        EncryptionMessage.Stage.NONE or _ => "None"
    };

    protected virtual async Task HandleMessageAsync(MessageBase message)
    {
        switch (message)
        {
            case SettingsMessage m:
                Settings = m.Settings;
                if (!Settings.UseEncryption)
                {
                    await _SendMessageAsync(new ConfirmationMessage(ConfirmationMessage.Confirmation.RESOLVED));
                    ConnectionState = ConnectionState.CONNECTED;
                    connectedSource.SetResult();
                    StartPoll();
                }
                else await SendMessageAsync(new ConfirmationMessage(ConfirmationMessage.Confirmation.ENCRYPTION));
                break;
            case EncryptionMessage m:
                encryptionStage = m.stage;
                if (encryptionStage == EncryptionMessage.Stage.SYN)
                {
                    _crypto.PublicKey = m.RSA;
                    _crypto.AesKey = CryptographyService.KeyFromHash(CryptographyService.CreateHash(Guid.NewGuid().ToByteArray()));
                    await _SendMessageAsync(new EncryptionMessage(_crypto.AesKey));
                }
                else if (encryptionStage == EncryptionMessage.Stage.ACK)
                {
                    _crypto.AesKey = m.AES;
                    await _SendMessageAsync(new EncryptionMessage(EncryptionMessage.Stage.SYNACK));
                    encryptionStage = EncryptionMessage.Stage.SYNACK;
                }
                else if (encryptionStage == EncryptionMessage.Stage.SYNACK)
                {
                    await _SendMessageAsync(new ConfirmationMessage(ConfirmationMessage.Confirmation.RESOLVED));
                    ConnectionState = ConnectionState.CONNECTED;
                    connectedSource.SetResult();
                    StartPoll();
                }
                break;
            case ConfirmationMessage m:
                switch (m.Confirm)
                {
                    case ConfirmationMessage.Confirmation.RESOLVED:
                        ConnectionState = ConnectionState.CONNECTED;
                        connectedSource.SetResult();
                        StartPoll();
                        break;
                    case ConfirmationMessage.Confirmation.ENCRYPTION:
                        CryptographyService.GenerateKeyPair(out RSAParameters Public, out RSAParameters p);
                        _crypto.PrivateKey = p;
                        await _SendMessageAsync(new EncryptionMessage(Public));
                        break;
                }
                break;
            case ConnectionPollMessage:
                _timedOut = false;
                break;
            default:
                var msgHandler = _MessageHandlers.FirstOrDefault(kv => kv.Key.Name.Equals(message.MessageType)).Value;
                if (msgHandler != null) msgHandler(message);
                else OnUnregisteredMessage?.Invoke(message);
                break;
        }
    }

    protected override async IAsyncEnumerable<MessageBase> ReceiveMessagesAsync()
    {
        const int buffer_length = 4096;
        byte[] buffer = new byte[buffer_length];
        List<byte> allBytes = new List<byte>();

        while (ConnectionState != ConnectionState.CLOSED)
        {
            try
            {
                int received;
                do
                {
                    received = await Connection.ReceiveToBufferAsync(buffer);
                    allBytes.AddRange(buffer[0..received]);
                }
                while (received == buffer_length);
            }
            catch (Exception ex)
            {
                if (ConnectionState != ConnectionState.CLOSED)
                    await DisconnectedEventAsync(new DisconnectionInfo 
                    {
                        Exception = ex
                    });
                yield break;
            }

            IEnumerable<MessageBase> messages;

            string encType = "None";
            if (Settings != null && Settings.UseEncryption)
                encType = encryptionStage switch
                {
                    EncryptionMessage.Stage.SYN => "Aes",
                    EncryptionMessage.Stage.ACK => "Rsa",
                    EncryptionMessage.Stage.SYNACK => "Aes",
                    _ => _crypto.PrivateKey == null ? "None" : "Rsa"
                };

            messages = MessageParser.DecapsulateMessages(allBytes, 
                new Dictionary<string, string>()
                {
                    { "Encryption", encType }
                });

            foreach (MessageBase msg in messages) 
                yield return msg;
        }
    }

    private protected abstract void CloseConnection();

    protected void Disconnected()
    {
        TokenSource.Cancel();
        _pollTimer?.Dispose();
        ConnectionState = ConnectionState.CLOSED;
        CloseConnection();

        encryptionStage = EncryptionMessage.Stage.NONE;
        Settings = null;
    }

    public override void Close() =>
        Utilities.ConcurrentAccess(() =>
        {
            if (ConnectionState == ConnectionState.CLOSED) return;

            _SendMessage(new DisconnectMessage());
            Disconnected();
        }, _semaphore);

    public override async Task CloseAsync() =>
        await Utilities.ConcurrentAccessAsync(async (ct) =>
        {
            if (ConnectionState == ConnectionState.CLOSED) return;

            await _SendMessageAsync(new DisconnectMessage());
            Disconnected();
        }, _semaphore);

    protected async Task DisconnectedEventAsync(DisconnectionInfo info) =>
        await Utilities.ConcurrentAccessAsync((c) =>
        {
            if (ConnectionState == ConnectionState.CLOSED) return Task.CompletedTask;

            Disconnected();
            OnDisconnect?.Invoke(info);
            return Task.CompletedTask;
        }, _semaphore);

    protected void DisconnectedEvent(DisconnectionInfo info) =>
        Utilities.ConcurrentAccess(() =>
        {
            if (ConnectionState == ConnectionState.CLOSED) return;

            Disconnected();
            OnDisconnect?.Invoke(info);
        }, _semaphore);

    private void StartPoll()
    {
        if (Settings.ConnectionPollTimeout <= 0)
            return;

        _pollTimer = new System.Timers.Timer(Settings.ConnectionPollTimeout);
        _pollTimer.Elapsed += (obj, args) =>
        {
            if (ConnectionState == ConnectionState.CONNECTED)
            {
                if (_timedOut)
                {
                    DisconnectedEvent(new DisconnectionInfo
                    {
                        Reason = "Connection timed out."
                    });
                    return;
                }
                SendMessage(new ConnectionPollMessage());
                _timedOut = true;
            }
        };
        _pollTimer.Start();
    }
}

/// <summary>
/// This represents an abstract client where you can set the main connection to any channel type.
/// </summary>
public abstract class GeneralClient : GeneralClient<IChannel>
{

}