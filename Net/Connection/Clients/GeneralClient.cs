﻿namespace Net.Connection.Clients;

using Channels;
using Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


public abstract class GeneralClient<Connection> : BaseClient where Connection : class, IChannel
{
    private SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    protected Invoker _invokationList = new();

    protected CancellationTokenSource TokenSource = new CancellationTokenSource();

    protected RSAParameters? RsaKey;
    protected volatile byte[] Key;

    protected volatile NetSettings Settings;

    protected abstract Connection connection { get; set; }
    /// <summary>
    /// The state of the current connection.
    /// </summary>
    public ConnectState ConnectionState { get; protected set; } = ConnectState.NONE;

    private EncryptionMessage.Stage _encryptionStage = EncryptionMessage.Stage.NONE;

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
    public readonly Dictionary<string, Action<MessageBase>> CustomMessageHandlers = new();

    public override void SendMessage(MessageBase message)
    {
        try
        {
            var bytes = MessageParser.Encapsulate(GetEncrypted(MessageParser.Serialize(message)));
            Utilities.ConcurrentAccess(() => connection.SendBytes(bytes), _sendSemaphore);
        }
        catch (Exception ex)
        {
            if (ConnectionState != ConnectState.CLOSED)
                DisconnectedEvent();
        }
    }

    public override async Task SendMessageAsync(MessageBase message, CancellationToken token = default)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token, TokenSource.Token);

        try
        {
            var bytes = MessageParser.Encapsulate(await GetEncryptedAsync(await MessageParser.SerializeAsync(message, cts.Token)));
            await Utilities.ConcurrentAccessAsync(async (ct) =>
                await connection.SendBytesAsync(bytes, cts.Token),
                _sendSemaphore);
        }
        catch
        {
            if (ConnectionState != ConnectState.CLOSED)
                await DisconnectedEventAsync();
        }
    }

    private byte[] GetEncrypted(byte[] bytes) => _encryptionStage switch
    {
        EncryptionMessage.Stage.SYN => CryptoServices.EncryptRSA(bytes, RsaKey.Value),
        EncryptionMessage.Stage.ACK => CryptoServices.EncryptAES(bytes, Key, Key),
        EncryptionMessage.Stage.SYNACK => CryptoServices.EncryptAES(bytes, Key, Key),
        EncryptionMessage.Stage.NONE or _ => bytes
    };

    private async Task<byte[]> GetEncryptedAsync(byte[] bytes) => _encryptionStage switch
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
                    SendMessage(new ConfirmationMessage(ConfirmationMessage.Confirmation.RESOLVED));
                    ConnectionState = ConnectState.CONNECTED;
                }
                else SendMessage(new ConfirmationMessage(ConfirmationMessage.Confirmation.ENCRYPTION));
                break;
            case EncryptionMessage m:
                _encryptionStage = m.stage;
                if (_encryptionStage == EncryptionMessage.Stage.SYN)
                {
                    RsaKey = m.RSA;
                    Key = CryptoServices.KeyFromHash(CryptoServices.CreateHash(Guid.NewGuid().ToByteArray()));
                    SendMessage(new EncryptionMessage(Key));
                }
                else if (_encryptionStage == EncryptionMessage.Stage.ACK)
                {
                    Key = m.AES;
                    SendMessage(new EncryptionMessage(EncryptionMessage.Stage.SYNACK));
                    _encryptionStage = EncryptionMessage.Stage.SYNACK;
                }
                else if (_encryptionStage == EncryptionMessage.Stage.SYNACK)
                {
                    SendMessage(new ConfirmationMessage(ConfirmationMessage.Confirmation.RESOLVED));
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

                        SendMessage(new EncryptionMessage(Public));
                        break;
                }
                break;
            default:
                var msgHandler = CustomMessageHandlers[message.MessageType];
                if (msgHandler != null) _invokationList.AddAction(() => msgHandler(message));
                else _invokationList.AddAction(() => OnUnregisteredMessage?.Invoke(message));
                break;
        }
    }

    protected override IEnumerable<MessageBase> ReceiveMessages()
    {
        List<byte> allBytes = new List<byte>();

        while (ConnectionState != ConnectState.CLOSED)
        {
            try
            {
                allBytes.AddRange(connection.ReceiveBytes());
            }
            catch
            {
                if (ConnectionState != ConnectState.CLOSED)
                    DisconnectedEvent();
                yield break;
            }

            IEnumerable<MessageBase> messages = null;

            if (Settings != null && Settings.UseEncryption)
                messages = _encryptionStage switch
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
        List<byte> allBytes = new List<byte>();

        while (ConnectionState != ConnectState.CLOSED)
        {
            try
            {
                allBytes.AddRange(await connection.ReceiveBytesAsync());
            }
            catch
            {
                if (ConnectionState != ConnectState.CLOSED)
                    await DisconnectedEventAsync();
                yield break;
            }

            IEnumerable<MessageBase> messages = null;

            if (Settings != null && Settings.UseEncryption)
                messages = _encryptionStage switch
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
        connection.Close();
        connection = null;

        foreach (var c in Channels)
            c.Close();

        Channels.Clear();
        _encryptionStage = EncryptionMessage.Stage.NONE;
        Settings = null;
        RsaKey = null;
        Key = null;
    }

    public override void Close() =>
        Utilities.ConcurrentAccess(() =>
        {
            if (ConnectionState == ConnectState.CLOSED) return;

            SendMessage(new DisconnectMessage());
            Soc.LingerState = new LingerOption(true, 1);
            Disconnected();
        }, _semaphore);

    public override async Task CloseAsync() =>
        await Utilities.ConcurrentAccessAsync(async (ct) =>
        {
            if (ConnectionState == ConnectState.CLOSED) return;

            await SendMessageAsync(new DisconnectMessage());
            Soc.LingerState = new LingerOption(true, 1);
            Disconnected();
        }, _semaphore);

    protected async Task DisconnectedEventAsync(bool graceful = false) =>
        await Utilities.ConcurrentAccessAsync((c) =>
        {
            if (ConnectionState == ConnectState.CLOSED) return Task.CompletedTask;

            Disconnected();
            Task.Run(() => OnDisconnect?.Invoke(graceful));
            return Task.CompletedTask;
        }, _semaphore);

    protected void DisconnectedEvent(bool graceful = false) =>
        Utilities.ConcurrentAccess(() =>
        {
            if (ConnectionState == ConnectState.CLOSED) return;

            Disconnected();
            Task.Run(() => OnDisconnect?.Invoke(graceful));
        }, _semaphore);
}