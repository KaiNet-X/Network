namespace Net.Connection.Clients.Tcp;

using Messages;
using Net.Connection.Channels;
using Net.Connection.Clients.General;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Base class for ObjectClient that impliments the underlying protocol
/// </summary>
public abstract class GeneralSocketClient : GeneralClient<TcpChannel>
{
    private SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private IPEndPoint _localEndpoint;
    private IPEndPoint _remoteEndpoint;
    protected Invoker _invokationList = new();

    protected CancellationTokenSource TokenSource = new CancellationTokenSource();

    protected RSAParameters? RsaKey;
    protected volatile byte[] Key;

    protected volatile NetSettings Settings;

    private volatile Socket Soc;

    /// <summary>
    /// Local endpoint
    /// </summary>
    public IPEndPoint LocalEndpoint
    {
        get
        {
            var ep = Soc?.LocalEndPoint as IPEndPoint;
            return ep != null ? (_localEndpoint = ep) : _localEndpoint;
        }
    }

    /// <summary>
    /// Remote endpoint
    /// </summary>
    public IPEndPoint RemoteEndpoint
    {
        get
        {
            var ep = Soc?.RemoteEndPoint as IPEndPoint;
            return ep != null ? (_remoteEndpoint = ep) : _remoteEndpoint;
        }
    }

    private EncryptionMessage.Stage _encryptionStage = EncryptionMessage.Stage.NONE;

    protected override IEnumerable<MessageBase> ReceiveMessages()
    {
        const int buffer_length = 1024;
        List<byte> allBytes = new List<byte>();
        ArraySegment<byte> buffer = new byte[buffer_length];

        while (ConnectionState != ConnectState.CLOSED)
        {
            try
            {
                Soc.Receive(buffer);
                int received;
                do
                {
                    received = Soc.Receive(buffer);
                    allBytes.AddRange(buffer.Take(received));
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
        const int buffer_length = 1024;
        List<byte> allBytes = new List<byte>();
        ArraySegment<byte> buffer = new byte[buffer_length];
        while (ConnectionState != ConnectState.CLOSED)
        {
            try
            {
                int received;
                do
                {
                    received = await Soc.ReceiveAsync(buffer, SocketFlags.None);
                    allBytes.AddRange(buffer.Take(received));
                }
                while (received == buffer_length);
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
}