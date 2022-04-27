namespace Net.Connection.Clients;

using Messages;
using Net.Connection.Channels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

public abstract class GeneralClient : ClientBase
{
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private IPEndPoint _localEndpoint;
    private IPEndPoint _remoteEndpoint;

    protected CancellationTokenSource TokenSource = new CancellationTokenSource();

    protected RSAParameters? RsaKey;
    protected volatile byte[] Key;

    protected volatile Socket Soc;

    public IPEndPoint LocalEndpoint
    {
        get
        {
            var ep = (Soc?.LocalEndPoint as IPEndPoint);
            return ep != null ? (_localEndpoint = ep) : _localEndpoint;
        }
    }
    public IPEndPoint RemoteEndpoint
    {
        get
        {
            var ep = (Soc?.RemoteEndPoint as IPEndPoint);
            return ep != null ? (_remoteEndpoint = ep) : _remoteEndpoint;
        }
    }
    public volatile Dictionary<Guid, Channel> Channels = new Dictionary<Guid, Channel>();

    protected bool AwaitingPoll;
    protected Stopwatch Timer;

    public ConnectState ConnectionState { get; protected set; } = ConnectState.NONE;

    private EncryptionMessage.Stage _encryptionStage = EncryptionMessage.Stage.NONE;

    public event Action<object> OnRecieveObject;
    public event Action<Guid> OnChannelOpened;
    public event Action<MessageBase> OnRecievedUnregisteredCustomMessage;
    public event Action<bool> OnDisconnect;

    public Dictionary<string, Action<MessageBase>> CustomMessageHandlers;

    public virtual void SendObject<T>(T obj) =>
        SendMessage(new ObjectMessage(obj));
   
    public virtual async Task SendObjectAsync<T>(T obj, CancellationToken token = default) =>
        await SendMessageAsync(new ObjectMessage(obj), token);

    public override void SendMessage(MessageBase message)
    {
        try
        {
            Soc.Send(MessageParser.AddTags(GetEncrypted(message.Serialize())).ToArray());
        }
        catch
        {
            DisconnectedEvent().GetAwaiter().GetResult();
        }
    }

    public override async Task SendMessageAsync(MessageBase message, CancellationToken token = default)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token, TokenSource.Token);

        try
        {
            await Soc.SendAsync(MessageParser.AddTags(GetEncrypted(await message.SerializeAsync(cts.Token))).ToArray(), SocketFlags.None, cts.Token);
        }
        catch
        {
            await DisconnectedEvent();
        }
    }

    private byte[] GetEncrypted(byte[] bytes) =>_encryptionStage switch
    {
        EncryptionMessage.Stage.SYN => CryptoServices.EncryptRSA(bytes, RsaKey.Value),
        EncryptionMessage.Stage.ACK => CryptoServices.EncryptAES(bytes, Key),
        EncryptionMessage.Stage.SYNACK => CryptoServices.EncryptAES(bytes, Key),
        EncryptionMessage.Stage.NONE => bytes
    };

    public Guid OpenChannel()
    {
        Channel c = new Channel((Soc.LocalEndPoint as IPEndPoint).Address) { AesKey = Key };
        Channels.Add(c.Id, c);
        SendMessage(new ChannelManagementMessage(c.Id, c.Port, ChannelManagementMessage.Mode.Create));
        return c.Id;
    }

    public async Task<Guid> OpenChannelAsync()
    {
        Channel c = new Channel((Soc.LocalEndPoint as IPEndPoint).Address) { AesKey = Key};
        Channels.Add(c.Id, c);
        await SendMessageAsync(new ChannelManagementMessage(c.Id, c.Port, ChannelManagementMessage.Mode.Create));
        return c.Id;
    }

    public void SendBytesOnChannel(byte[] bytes, Guid id) =>
        Channels[id].SendBytes(bytes);

    public async Task SendBytesOnChannelAsync(byte[] bytes, Guid id, CancellationToken token = default)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token, TokenSource.Token);
        await Channels[id].SendBytesAsync(bytes, cts.Token);
    }

    public byte[] RecieveBytesFromChannel(Guid id) =>
        Channels[id].RecieveBytes();

    public async Task<byte[]> RecieveBytesFromChannelAsync(Guid id, CancellationToken token = default)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token, TokenSource.Token);

        return await Channels[id].RecieveBytesAsync(cts.Token);
    }

    internal void StartConnectionPoll()
    {
        if (_encryptionStage != EncryptionMessage.Stage.SYNACK && _encryptionStage != EncryptionMessage.Stage.NONE) return;
        if (ConnectionState != ConnectState.CONNECTED) return;
        AwaitingPoll = true;
        (Timer ??= new Stopwatch()).Start();

        SendMessage(new ConnectionPollMessage { PollState = ConnectionPollMessage.PollMessage.SYN });
    }

    private void PollConnected()
    {
        Timer?.Reset();
        AwaitingPoll = false;
    }

    private void Disconnected()
    {
        Timer?.Stop();
        Timer = null;
        AwaitingPoll = false;

        TokenSource.Cancel();

        ConnectionState = ConnectState.CLOSED;
        Soc.Close();
        Soc = null;

        foreach (var c in Channels)
            c.Value.Dispose();

        Channels.Clear();
        _encryptionStage = EncryptionMessage.Stage.NONE;
        Settings = null;
        RsaKey = null;
        Key = null;
    }

    protected async Task HandleMessage(MessageBase message)
    {
        switch (message)
        {
            case ConfirmationMessage m:
                switch (m.GetValue() as string)
                {
                    case "resolved":
                        ConnectionState = ConnectState.CONNECTED;
                        break;
                    case "encryption":
                        CryptoServices.GenerateKeyPair(out RSAParameters Public, out RSAParameters p);
                        RsaKey = p;

                        SendMessage(new EncryptionMessage(Public));
                        break;
                }
                break;
            case ObjectMessage m:
                Task.Run(() => OnRecieveObject?.Invoke(m.GetValue()));
                break;
            case SettingsMessage m:
                Settings = m.GetValue() as NetSettings;
                if (!Settings.UseEncryption)
                {
                    SendMessage(new ConfirmationMessage("resolved"));
                    ConnectionState = ConnectState.CONNECTED;
                }
                else SendMessage(new ConfirmationMessage("encryption"));
                break;
            case EncryptionMessage m:
                _encryptionStage = m.stage;
                if (_encryptionStage == EncryptionMessage.Stage.SYN)
                {
                    RsaKey = (RSAParameters)m.GetValue();
                    Key = CryptoServices.KeyFromHash(CryptoServices.CreateHash(Guid.NewGuid().ToByteArray()));
                    SendMessage(new EncryptionMessage(Key));
                }
                else if (_encryptionStage == EncryptionMessage.Stage.ACK)
                {
                    Key = m.GetValue() as byte[];
                    SendMessage(new EncryptionMessage(EncryptionMessage.Stage.SYNACK));
                    _encryptionStage = EncryptionMessage.Stage.SYNACK;
                }
                else if (_encryptionStage == EncryptionMessage.Stage.SYNACK)
                {
                    SendMessage(new ConfirmationMessage("resolved"));
                    ConnectionState = ConnectState.CONNECTED;
                }
                break;
            case ChannelManagementMessage m:
                var val = (Guid)m.GetValue();
                if (m.ManageMode == ChannelManagementMessage.Mode.Create)
                {
                    var ipAddr = (Soc.LocalEndPoint as IPEndPoint).Address;
                    var remoteEndpoint = new IPEndPoint((Soc.RemoteEndPoint as IPEndPoint).Address, m.Port);
                    var c = new Channel(ipAddr, remoteEndpoint, val) { AesKey = Key };
                    c.Connected = true;
                    Channels.Add(c.Id, c);
                    SendMessage(new ChannelManagementMessage(val, c.Port, ChannelManagementMessage.Mode.Confirm));
                    Task.Run(() => OnChannelOpened?.Invoke(val));
                }
                else if (m.ManageMode == ChannelManagementMessage.Mode.Confirm)
                {
                    var c = Channels[val];
                    c.SetRemote(new IPEndPoint((Soc.RemoteEndPoint as IPEndPoint).Address, m.Port));
                    c.Connected = true;
                }
                break;
            case ConnectionPollMessage m:
                switch (m.PollState)
                {
                    case ConnectionPollMessage.PollMessage.SYN:
                        SendMessage(new ConnectionPollMessage { PollState = ConnectionPollMessage.PollMessage.ACK });
                        break;
                    case ConnectionPollMessage.PollMessage.ACK:
                        PollConnected();
                        break;
                    case ConnectionPollMessage.PollMessage.DISCONNECT:
                        await DisconnectedEvent(true);
                        break;
                }
                break;
            default:
                Task.Run(() =>
                {
                    var msgHandler = CustomMessageHandlers[message.MessageType];
                    if (msgHandler != null) msgHandler(message);
                    else OnRecievedUnregisteredCustomMessage?.Invoke(message);
                });
                break;
        }
    }

    protected override IEnumerable<MessageBase> RecieveMessages()
    {
        List<byte> allBytes = new List<byte>();
        byte[] buffer;

        while (true)
        {
            if (ConnectionState == ConnectState.CLOSED) 
                yield break;

            int available;

            if (AwaitingPoll && Timer?.ElapsedMilliseconds >= Settings.ConnectionPollTimeout)
            {
                DisconnectedEvent().GetAwaiter().GetResult();
                yield break;
            }

            available = Soc.Available;
            if (available == 0)
            {
                yield return null;
                continue;
            }

            PollConnected();

            buffer = new byte[available];
            Task.Delay(10).Wait();

            try
            {
                Soc.Receive(buffer);
            }
            catch
            {
                DisconnectedEvent().GetAwaiter().GetResult();
                yield break;
            }

            allBytes.AddRange(buffer);
            List<MessageBase> messages = null;

            if (Settings != null && Settings.UseEncryption)
                switch (_encryptionStage)
                {
                    case EncryptionMessage.Stage.SYN:
                        messages = MessageParser.GetMessagesAes(ref allBytes, Key);
                        break;
                    case EncryptionMessage.Stage.ACK:
                        messages = MessageParser.GetMessagesRsa(ref allBytes, RsaKey.Value);
                        break;
                    case EncryptionMessage.Stage.SYNACK:
                        messages = MessageParser.GetMessagesAes(ref allBytes, Key);
                        break;
                    default:
                        if (RsaKey == null)
                            messages = MessageParser.GetMessages(ref allBytes);
                        else
                            messages = MessageParser.GetMessagesRsa(ref allBytes, RsaKey.Value);
                        break;
                }
            else messages = MessageParser.GetMessages(ref allBytes);

            foreach (MessageBase msg in messages) yield return msg;
        }
    }

    public void Close() =>
        Task.Run(async () => await CloseAsync()).GetAwaiter().GetResult();

    public async Task CloseAsync() =>
        await Utilities.ConcurrentAccess(async (ct) =>
        {
            if (ConnectionState == ConnectState.CLOSED) return;
            await SendMessageAsync(new ConnectionPollMessage { PollState = ConnectionPollMessage.PollMessage.DISCONNECT });
            Soc.LingerState = new LingerOption(true, 1);
            Disconnected();
        }, _semaphore);

    private async Task DisconnectedEvent(bool graceful = false)
    {
        await Utilities.ConcurrentAccess((c) =>
        {
            if (ConnectionState == ConnectState.CLOSED) return Task.CompletedTask;

            Disconnected();
            Task.Run(() => OnDisconnect?.Invoke(graceful));
            return Task.CompletedTask;
        }, _semaphore);
    }
}

public enum ConnectState
{
    NONE,
    PENDING,
    CONNECTED,
    CLOSED
}