namespace Net.Connection.Clients.Generic;

using Channels;
using Messages;
using Net;
using Net.Connection.Servers;
using Net.Messages.Parsing;
using Net.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

public abstract class GeneralClient<MainChannel> : BaseClient where MainChannel : class, IChannel
{
    private SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private CryptographyService _crypto = new();

    private IMessageParser _parser;

    /// <summary>
    /// Message parser the library uses (by default, NewMessageParser with MpSerializer
    /// </summary>
    public IMessageParser MessageParser { get => _parser ??= new NewMessageParser(_crypto, Consts.DefaultSerializer); }

    private bool _timedOut = false;

    protected CancellationTokenSource TokenSource = new CancellationTokenSource();

    protected volatile ServerSettings Settings;

    protected MainChannel Connection { get; set; }

    private System.Timers.Timer _pollTimer;

    /// <summary>
    /// The state of the current connection.
    /// </summary>
    public ConnectState ConnectionState { get; protected set; } = ConnectState.NONE;

    private EncryptionMessage.Stage encryptionStage = EncryptionMessage.Stage.NONE;

    /// <summary>
    /// Invoked when an unregistered message is received
    /// </summary>
    public event Action<MessageBase> OnUnregisteredMessage;

    /// <summary>
    /// Invoked when disconnected from. Argument is graceful or ungraceful. 
    /// </summary>
    public event Action<bool> OnDisconnect;

    /// <summary>
    /// Register message handlers for custom message types with message type name
    /// </summary>
    protected readonly Dictionary<Type, Action<MessageBase>> _CustomMessageHandlers = new();

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
            if (ConnectionState != ConnectState.CLOSED)
                DisconnectedEvent();
        }
        finally
        {
            _sendSemaphore.Release();
        }
    }

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
        catch
        {
            if (ConnectionState != ConnectState.CLOSED)
                await DisconnectedEventAsync();
        }
    }

    public override async Task SendMessageAsync(MessageBase message, CancellationToken token = default) =>
        await _SendMessageAsync(message, token);

    public void RegisterMessageHandler<T>(Action<T> handler) where T : MessageBase =>
        _CustomMessageHandlers.Add(typeof(T), mb => handler((T)mb));

    public void RegisterMessageHandler(Action<MessageBase> handler, Type messageType) =>
        _CustomMessageHandlers.Add(messageType, mb => handler(mb));

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
                    ConnectionState = ConnectState.CONNECTED;
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
                    ConnectionState = ConnectState.CONNECTED;
                    StartPoll();
                }
                break;
            case ConfirmationMessage m:
                switch (m.Confirm)
                {
                    case ConfirmationMessage.Confirmation.RESOLVED:
                        ConnectionState = ConnectState.CONNECTED;
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
                var msgHandler = _CustomMessageHandlers.FirstOrDefault(kv => kv.Key.Name.Equals(message.MessageType)).Value;
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

        while (ConnectionState != ConnectState.CLOSED)
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
                if (ConnectionState != ConnectState.CLOSED)
                    await DisconnectedEventAsync();
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

            messages = MessageParser.DecapsulateMessages(allBytes, new Dictionary<string, string>()
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
        _pollTimer.Dispose();
        ConnectionState = ConnectState.CLOSED;
        CloseConnection();

        encryptionStage = EncryptionMessage.Stage.NONE;
        Settings = null;
    }

    public override void Close() =>
        Utilities.ConcurrentAccess(() =>
        {
            if (ConnectionState == ConnectState.CLOSED) return;

            _SendMessage(new DisconnectMessage());
            Disconnected();
        }, _semaphore);

    public override async Task CloseAsync() =>
        await Utilities.ConcurrentAccessAsync(async (ct) =>
        {
            if (ConnectionState == ConnectState.CLOSED) return;

            await _SendMessageAsync(new DisconnectMessage());
            Disconnected();
        }, _semaphore);

    protected async Task DisconnectedEventAsync(bool graceful = false) =>
        await Utilities.ConcurrentAccessAsync((c) =>
        {
            if (ConnectionState == ConnectState.CLOSED) return Task.CompletedTask;

            Disconnected();
            OnDisconnect?.Invoke(graceful);
            return Task.CompletedTask;
        }, _semaphore);

    protected void DisconnectedEvent(bool graceful = false) =>
        Utilities.ConcurrentAccess(() =>
        {
            if (ConnectionState == ConnectState.CLOSED) return;

            Disconnected();
            OnDisconnect?.Invoke(graceful);
        }, _semaphore);

    private void StartPoll()
    {
        _pollTimer = new System.Timers.Timer(Settings.ConnectionPollTimeout);
        _pollTimer.Elapsed += (obj, args) =>
        {
            if (ConnectionState == ConnectState.CONNECTED)
            {
                if (_timedOut)
                {
                    DisconnectedEvent(false);
                    return;
                }
                SendMessage(new ConnectionPollMessage());
                _timedOut = true;
            }
        };
        _pollTimer.Start();
    }
}

public abstract class GeneralClient : GeneralClient<IChannel>
{

}