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
    private EncryptionStage encryptionStage = EncryptionStage.NONE;
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

    };

    /// <summary>
    /// Register asynchronous message handlers for custom message types with message type name
    /// </summary>
    protected readonly Dictionary<Type, Func<MessageBase, Task>> _AsyncMessageHandlers = new()
    {

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
    public override void SendMessage(MessageBase message) =>
        _SendMessage(message);

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
    /// <param name="token"></param>
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
    /// Generic method to register an action to handle the specified method type.
    /// </summary>
    /// <typeparam name="T">Type of the message</typeparam>
    /// <param name="handler">Action that is called whenever the message is received.</param>
    public void RegisterAsyncMessageHandler<T>(Func<T, Task> handler) where T : MessageBase =>
        RegisterAsyncMessageHandler(mb => handler(mb as T), typeof(T));

    /// <summary>
    /// Non-generic method to register an action to handle the specified method type.
    /// </summary>
    /// <param name="handler">Action that is called whenever the message is received.</param>
    /// <param name="messageType">Type of the message to register.</param>
    public void RegisterMessageHandler(Action<MessageBase> handler, Type messageType) =>
        _MessageHandlers.Add(messageType, handler);

    /// <summary>
    /// Non-generic method to register an action to handle the specified method type.
    /// </summary>
    /// <param name="handler">Action that is called whenever the message is received.</param>
    /// <param name="messageType">Type of the message to register.</param>
    public void RegisterAsyncMessageHandler(Func<MessageBase, Task> handler, Type messageType) =>
        _AsyncMessageHandlers.Add(messageType, handler);

    private string GetEnc() => encryptionStage switch
    {
        EncryptionStage.SYN => "Rsa",
        EncryptionStage.ACK => "Aes",
        EncryptionStage.SYNACK => "Aes",
        EncryptionStage.NONE or _ => "None"
    };

    protected virtual async Task HandleMessageAsync(MessageBase message)
    {
        _timedOut = false;
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
                else 
                    await SendMessageAsync(new ConfirmationMessage(ConfirmationMessage.Confirmation.ENCRYPTION));
                break;
            case EncryptionMessage m:
                encryptionStage = m.Stage;
                if (encryptionStage == EncryptionStage.SYN)
                {
                    _crypto.PublicKey = m.RsaPair;
                    await _SendMessageAsync(new EncryptionMessage(_crypto.AesKey, _crypto.AesIv));
                }
                else if (encryptionStage == EncryptionStage.ACK)
                {
                    _crypto.AesKey = m.AesKey;
                    _crypto.AesIv = m.AesIv;
                    await _SendMessageAsync(new EncryptionMessage(EncryptionStage.SYNACK));
                    encryptionStage = EncryptionStage.SYNACK;
                }
                else if (encryptionStage == EncryptionStage.SYNACK)
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
            case ConnectionPollMessage m:
                if (!m.IsResponse) await _SendMessageAsync(new ConnectionPollMessage(true));
                break;
            default:
                var asyncMsgHandler = _AsyncMessageHandlers.FirstOrDefault(kv => kv.Key.Name.Equals(message.MessageType)).Value;
                var msgHandler = _MessageHandlers.FirstOrDefault(kv => kv.Key.Name.Equals(message.MessageType)).Value;
                if (asyncMsgHandler == null && msgHandler == null) OnUnregisteredMessage?.Invoke(message);
                else
                {
                    if (asyncMsgHandler != null) await asyncMsgHandler(message);
                    if (msgHandler != null) msgHandler(message);
                }
                break;
        }
    }

    /// <summary>
    /// Streams messages as they are parsed from the connection
    /// </summary>
    /// <returns></returns>
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
                        Reason = DisconnectionReason.Aborted,
                        Exception = ex
                    });
                yield break;
            }

            IEnumerable<MessageBase> messages;

            string encType = "None";
            if (Settings != null && Settings.UseEncryption)
                encType = encryptionStage switch
                {
                    EncryptionStage.SYN => "Aes",
                    EncryptionStage.ACK => "Rsa",
                    EncryptionStage.SYNACK => "Aes",
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
        TokenSource.Dispose();
        TokenSource = new CancellationTokenSource();
        _pollTimer?.Dispose();
        ConnectionState = ConnectionState.CLOSED;
        CloseConnection();

        encryptionStage = EncryptionStage.NONE;
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
            DisconnectedEventLogic(info);
            return Task.CompletedTask;
        }, _semaphore);

    protected void DisconnectedEvent(DisconnectionInfo info) =>
        Utilities.ConcurrentAccess(() =>
        {
            DisconnectedEventLogic(info);
        }, _semaphore);

    private void DisconnectedEventLogic(DisconnectionInfo info)
    {
        if (ConnectionState == ConnectionState.CLOSED) return;
        if (info.Reason == DisconnectionReason.TimedOut)
        Disconnected();
        OnDisconnect?.Invoke(info);
    }

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
                        Reason = DisconnectionReason.TimedOut
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