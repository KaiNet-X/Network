namespace Net.Connection.Clients.Generic;

using Channels;
using Messages;
using Net;
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
public abstract class GeneralClient<MainChannel> : BaseClient where MainChannel : BaseChannel
{
    protected readonly CryptographyService _crypto = new();

    private bool _timedOut = false;
    private System.Timers.Timer _pollTimer;
    private EncryptionStage encryptionStage = EncryptionStage.NONE;

    protected SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
    protected SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    protected CancellationTokenSource DisconnectTokenSource = new CancellationTokenSource();
    protected ConnectionSettings Settings;

    /// <summary>
    /// This channel represents the connection for this server. It is best to use a reliable protocol such at TCP over one like UDP for the main connection.
    /// </summary>
    protected MainChannel Connection { get; set; }

    /// <summary>
    /// Register asynchronous message handlers for custom message types with message type name
    /// </summary>
    protected readonly Dictionary<Type, Func<MessageBase, Task>> _MessageHandlers = new()
    {

    };

    /// <summary>
    /// Task that completes when the connection is finished. Call this in inherrited classes to asynchrounously complete the connection.
    /// </summary>
    protected TaskCompletionSource ConnectedTask { get; set; } = new TaskCompletionSource();

    /// <summary>
    /// The state of the current connection.
    /// </summary>
    public ConnectionState ConnectionState { get; protected set; } = ConnectionState.NONE;

    /// <summary>
    /// Message parser the library uses (by default, NewMessageParser with MpSerializer
    /// </summary>
    public IMessageParser MessageParser { get; init; }

    /// <summary>
    /// Invoked when an unregistered message is received
    /// </summary>
    private Func<MessageBase, Task> UnregisteredMessage;

    protected Func<DisconnectionInfo, Task> OnDisconnect;

    /// <summary>
    /// Sends a message to the remote client.
    /// </summary>
    /// <param name="message"></param>
    public override void SendMessage(MessageBase message)
    {
        if (ConnectionState == ConnectionState.CLOSED) return;
        else if (!Connection.Connected)
        {
            DisconnectedEvent(new DisconnectionInfo
            {
                Exception = Connection.ConnectionException
            });
        }

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
    /// <param name="token"></param>
    public override async Task SendMessageAsync(MessageBase message, CancellationToken token = default)
    {
        if (ConnectionState == ConnectionState.CLOSED) return;
        else if (!Connection.Connected)
        {
            DisconnectedEvent(new DisconnectionInfo
            {
                Exception = Connection.ConnectionException
            });
        }
        
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token, DisconnectTokenSource.Token);

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

    public void OnDisconnected(Func<DisconnectionInfo, Task> onDisconnect) =>
        OnDisconnect = onDisconnect;

    public void OnDisconnected(Action<DisconnectionInfo> onDisconnect) =>
        OnDisconnected(Utilities.SyncToAsync(onDisconnect));

    public void OnAnyMessage(Func<MessageBase, Task> handler) => 
        UnregisteredMessage = handler;

    public void OnAnyMessage(Action<MessageBase> handler) =>
        OnAnyMessage(Utilities.SyncToAsync(handler));

    /// <summary>
    /// Generic method to register an action to handle the specified method type.
    /// </summary>
    /// <typeparam name="T">Type of the message</typeparam>
    /// <param name="handler">Action that is called whenever the message is received.</param>
    public void OnMessageReceived<T>(Action<T> handler) where T : MessageBase =>
        OnMessageReceived(typeof(T), mb => handler(mb as T));

    /// <summary>
    /// Generic method to register an action to handle the specified method type.
    /// </summary>
    /// <typeparam name="T">Type of the message</typeparam>
    /// <param name="handler">Action that is called whenever the message is received.</param>
    public void OnMessageReceived<T>(Func<T, Task> handler) where T : MessageBase =>
        OnMessageReceived(typeof(T), mb => handler(mb as T));

    /// <summary>
    /// Non-generic method to register an action to handle the specified method type.
    /// </summary>
    /// <param name="messageType">Type of the message to register.</param>
    /// <param name="handler">Action that is called whenever the message is received.</param>
    public void OnMessageReceived(Type messageType, Action<MessageBase> handler) =>
        OnMessageReceived(messageType, Utilities.SyncToAsync(handler));

    /// <summary>
    /// Non-generic method to register an action to handle the specified method type.
    /// </summary>
    /// <param name="messageType">Type of the message to register.</param>
    /// <param name="handler">Action that is called whenever the message is received.</param>
    public void OnMessageReceived(Type messageType, Func<MessageBase, Task> handler) =>
        _MessageHandlers.TryAdd(messageType, handler);

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
                    await SendMessageAsync(new ConfirmationMessage(ConfirmationMessage.Confirmation.RESOLVED));
                    ConnectionState = ConnectionState.CONNECTED;
                    ConnectedTask.SetResult();
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
                    await SendMessageAsync(new EncryptionMessage(_crypto.AesKey, _crypto.AesIv));
                }
                else if (encryptionStage == EncryptionStage.ACK)
                {
                    _crypto.AesKey = m.AesKey;
                    _crypto.AesIv = m.AesIv;
                    await SendMessageAsync(new EncryptionMessage(EncryptionStage.SYNACK));
                    encryptionStage = EncryptionStage.SYNACK;
                }
                else if (encryptionStage == EncryptionStage.SYNACK)
                {
                    await SendMessageAsync(new ConfirmationMessage(ConfirmationMessage.Confirmation.RESOLVED));
                    ConnectionState = ConnectionState.CONNECTED;
                    ConnectedTask.SetResult();
                    StartPoll();
                }
                break;
            case ConfirmationMessage m:
                switch (m.Confirm)
                {
                    case ConfirmationMessage.Confirmation.RESOLVED:
                        ConnectionState = ConnectionState.CONNECTED;
                        ConnectedTask.SetResult();
                        StartPoll();
                        break;
                    case ConfirmationMessage.Confirmation.ENCRYPTION:
                        CryptographyService.GenerateKeyPair(out RSAParameters Public, out RSAParameters p);
                        _crypto.PrivateKey = p;
                        await SendMessageAsync(new EncryptionMessage(Public));
                        break;
                }
                break;
            case ConnectionPollMessage m:
                if (!m.IsResponse) await SendMessageAsync(new ConnectionPollMessage(true));
                break;
            default:
                var msgHandler = _MessageHandlers.FirstOrDefault(kv => kv.Key.Name.Equals(message.MessageType)).Value;
                if (msgHandler == null) UnregisteredMessage?.Invoke(message);
                else if (msgHandler != null) await msgHandler(message);
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
        var token = DisconnectTokenSource.Token;

        while (ConnectionState != ConnectionState.CLOSED)
        {
            try
            {
                int received;
                do
                {
                    received = await Connection.ReceiveToBufferAsync(buffer, token);
                    allBytes.AddRange(buffer[0..received]);
                }
                while (received == buffer_length && ConnectionState != ConnectionState.CLOSED && !token.IsCancellationRequested);
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

    private void Disconnected()
    {
        ConnectedTask.TrySetCanceled();
        ConnectedTask = new TaskCompletionSource();
        ConnectionState = ConnectionState.CLOSED;
        encryptionStage = EncryptionStage.NONE;
        DisconnectTokenSource.Cancel();
        DisconnectTokenSource.Dispose();
        DisconnectTokenSource = new CancellationTokenSource();
        _pollTimer?.Dispose();
        _pollTimer = null;
        CloseConnection();
    }

    public override void Close() =>
        Utilities.ConcurrentAccess(() =>
        {
            if (ConnectionState == ConnectionState.CLOSED) return;

            SendMessage(new DisconnectMessage());
            Disconnected();
        }, _semaphore);

    public override async Task CloseAsync() =>
        await Utilities.ConcurrentAccessAsync(async (ct) =>
        {
            if (ConnectionState == ConnectionState.CLOSED) return;

            await SendMessageAsync(new DisconnectMessage());
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
public abstract class GeneralClient : GeneralClient<BaseChannel>
{

}