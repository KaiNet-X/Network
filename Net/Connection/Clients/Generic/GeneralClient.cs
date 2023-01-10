namespace Net.Connection.Clients.Generic;

using Channels;
using Messages;
using Net;
using Net.Messages.Parser;
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
    protected Invoker _invokationList = new();

    protected CancellationTokenSource TokenSource = new CancellationTokenSource();

    protected RSAParameters? RsaKey;
    protected volatile byte[] Key;

    protected volatile NetSettings Settings;

    protected MainChannel Connection { get; set; }

    /// <summary>
    /// The state of the current connection.
    /// </summary>
    public ConnectState ConnectionState { get; protected set; } = ConnectState.NONE;

    protected EncryptionMessage.Stage encryptionStage = EncryptionMessage.Stage.NONE;

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

    public List<IChannel> Channels = new();

    private void _SendMessage(MessageBase message)
    {
        try
        {
            var bytes = MessageParser.Encapsulate(GetEncrypted(MessageParser.Serialize(message)));
            Utilities.ConcurrentAccess(() => Connection.SendBytes(bytes), _sendSemaphore);
        }
        catch (Exception ex)
        {
            if (ConnectionState != ConnectState.CLOSED)
                DisconnectedEvent();
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
            var bytes = MessageParser.Encapsulate(await GetEncryptedAsync(await MessageParser.SerializeAsync(message, cts.Token)));
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

    public override async Task SendMessageAsync(MessageBase message, CancellationToken token = default)
    {
        await _SendMessageAsync(message, token);
    }

    public void RegisterMessageHandler<T>(Action<T> handler) where T : MessageBase =>
        _CustomMessageHandlers.Add(typeof(T), mb => handler((T)mb));

    public void RegisterMessageHandler(Action<MessageBase> handler, Type messageType) =>
        _CustomMessageHandlers.Add(messageType, mb => handler(mb));

    private byte[] GetEncrypted(byte[] bytes) => encryptionStage switch
    {
        EncryptionMessage.Stage.SYN => CryptoServices.EncryptRSA(bytes, RsaKey.Value),
        EncryptionMessage.Stage.ACK => CryptoServices.EncryptAES(bytes, Key, Key),
        EncryptionMessage.Stage.SYNACK => CryptoServices.EncryptAES(bytes, Key, Key),
        EncryptionMessage.Stage.NONE or _ => bytes
    };

    private async Task<byte[]> GetEncryptedAsync(byte[] bytes) => encryptionStage switch
    {
        EncryptionMessage.Stage.SYN => CryptoServices.EncryptRSA(bytes, RsaKey.Value),
        EncryptionMessage.Stage.ACK => await CryptoServices.EncryptAESAsync(bytes, Key, Key),
        EncryptionMessage.Stage.SYNACK => await CryptoServices.EncryptAESAsync(bytes, Key, Key),
        EncryptionMessage.Stage.NONE or _ => bytes
    };

    protected virtual void HandleMessage(MessageBase message)
    {
        switch (message)
        {
            case SettingsMessage m:
                Settings = m.Settings;
                if (!Settings.UseEncryption)
                {
                    _SendMessage(new ConfirmationMessage(ConfirmationMessage.Confirmation.RESOLVED));
                    ConnectionState = ConnectState.CONNECTED;
                }
                else _SendMessage(new ConfirmationMessage(ConfirmationMessage.Confirmation.ENCRYPTION));
                break;
            case EncryptionMessage m:
                encryptionStage = m.stage;
                if (encryptionStage == EncryptionMessage.Stage.SYN)
                {
                    RsaKey = m.RSA;
                    Key = CryptoServices.KeyFromHash(CryptoServices.CreateHash(Guid.NewGuid().ToByteArray()));
                    _SendMessage(new EncryptionMessage(Key));
                }
                else if (encryptionStage == EncryptionMessage.Stage.ACK)
                {
                    Key = m.AES;
                    _SendMessage(new EncryptionMessage(EncryptionMessage.Stage.SYNACK));
                    encryptionStage = EncryptionMessage.Stage.SYNACK;
                }
                else if (encryptionStage == EncryptionMessage.Stage.SYNACK)
                {
                    _SendMessage(new ConfirmationMessage(ConfirmationMessage.Confirmation.RESOLVED));
                    ConnectionState = ConnectState.CONNECTED;
                }
                break;
            case ConfirmationMessage m:
                switch (m.Confirm)
                {
                    case ConfirmationMessage.Confirmation.RESOLVED:
                        ConnectionState = ConnectState.CONNECTED;
                        break;
                    case ConfirmationMessage.Confirmation.ENCRYPTION:
                        CryptoServices.GenerateKeyPair(out RSAParameters Public, out RSAParameters p);
                        RsaKey = p;
                        _SendMessage(new EncryptionMessage(Public));
                        break;
                }
                break;
            default:
                var msgHandler = _CustomMessageHandlers.FirstOrDefault(kv => kv.Key.Name.Equals(message.MessageType)).Value;
                if (msgHandler != null) msgHandler(message);
                else OnUnregisteredMessage?.Invoke(message);
                break;
        }
    }

    protected override IEnumerable<MessageBase> ReceiveMessages()
    {
        const int buffer_length = 1024;
        byte[] buffer = new byte[buffer_length];
        List<byte> allBytes = new List<byte>();

        while (ConnectionState != ConnectState.CLOSED)
        {
            try
            {
                int received;
                do
                {
                    received = Connection.ReceiveToBuffer(buffer);
                    allBytes.AddRange(buffer[0..received]);
                }
                while (received == buffer_length);
            }
            catch
            {
                if (ConnectionState != ConnectState.CLOSED)
                    DisconnectedEvent();
                yield break;
            }

            IEnumerable<MessageBase> messages = null;

            if (Settings != null && Settings.UseEncryption)
                messages = encryptionStage switch
                {
                    EncryptionMessage.Stage.SYN => MessageParser.GetMessagesAesEnum(allBytes, Key),
                    EncryptionMessage.Stage.ACK => MessageParser.GetMessagesRsaEnum(allBytes, RsaKey.Value),
                    EncryptionMessage.Stage.SYNACK => MessageParser.GetMessagesAesEnum(allBytes, Key),
                    _ => RsaKey == null ? MessageParser.GetMessagesEnum(allBytes) : MessageParser.GetMessagesRsaEnum(allBytes, RsaKey.Value)
                };
            else messages = MessageParser.GetMessagesEnum(allBytes);

            foreach (MessageBase msg in messages) yield return msg;
        }
    }

    protected override async IAsyncEnumerable<MessageBase> ReceiveMessagesAsync()
    {
        const int buffer_length = 1024;
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

            if (Settings != null && Settings.UseEncryption)
                messages = encryptionStage switch
                {
                    EncryptionMessage.Stage.SYN => MessageParser.GetMessagesAesEnum(allBytes, Key),
                    EncryptionMessage.Stage.ACK => MessageParser.GetMessagesRsaEnum(allBytes, RsaKey.Value),
                    EncryptionMessage.Stage.SYNACK => MessageParser.GetMessagesAesEnum(allBytes, Key),
                    _ => RsaKey == null ? MessageParser.GetMessagesEnum(allBytes) : MessageParser.GetMessagesRsaEnum(allBytes, RsaKey.Value)
                };
            else messages = MessageParser.GetMessagesEnum(allBytes);

            foreach (MessageBase msg in messages) yield return msg;
        }
    }

    protected void Disconnected()
    {
        TokenSource.Cancel();

        ConnectionState = ConnectState.CLOSED;
        Connection.Close();
        Connection = null;

        foreach (var c in Channels)
            c.Close();

        Channels.Clear();
        encryptionStage = EncryptionMessage.Stage.NONE;
        Settings = null;
        RsaKey = null;
        Key = null;
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
}

public abstract class GeneralClient : GeneralClient<IChannel>
{

}