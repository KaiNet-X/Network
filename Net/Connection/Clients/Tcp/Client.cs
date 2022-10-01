namespace Net.Connection.Clients.Tcp;

using Channels;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// The out-of-the-box Client implementation allows sending objects to the server, managing UDP channels, and follows an event based approach to receiving data.
/// </summary>
public class Client : ObjectClient
{
    /// <summary>
    /// Delay between client updates; highly reduces CPU usage
    /// </summary>
    public ushort LoopDelay = 1;
    private readonly IPEndPoint _targetEndpoint;

    public Task _listener { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="address">IP address of server</param>
    /// <param name="port">Server port the client will connect to</param>
    public Client(IPAddress address, int port) : this(new IPEndPoint(address, port)) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="address">IP address of server</param>
    /// <param name="port">Server port the client will connect to</param>
    public Client(string address, int port) : this(IPAddress.Parse(address), port) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ep">IPEndpoint of the server</param>
    public Client(IPEndPoint ep) : base()
    {
        ConnectionState = ConnectState.PENDING;
        _targetEndpoint = ep;

        Initialize();
    }

    /// <summary>
    /// Connect to the server this client is bound to
    /// </summary>
    /// <param name="maxAttempts">Max amount of connection attempts</param>
    /// <param name="throwWhenExausted">Throw exception if connection didn't work</param>
    /// <returns>true if connected, otherwise false</returns>
    public bool Connect(int maxAttempts = 0, bool throwWhenExausted = false)
    {
        if (Connection == null) Initialize();

        for (int i = 0; i <= maxAttempts; i++)
        {
            try
            {
                Connection.Socket.Connect(_targetEndpoint);
                StartLoop();
                break;
            }
            catch
            {
                if (i == maxAttempts)
                    if (throwWhenExausted)
                        throw;
                    else
                        return false;
            }
        }

        localEndPoint = Connection.Socket.LocalEndPoint as IPEndPoint;
        remoteEndPoint = Connection.Socket.RemoteEndPoint as IPEndPoint;

        while (ConnectionState == ConnectState.PENDING) ;
        return true;
    }

    /// <summary>
    /// Connect to the server this client is bound to
    /// </summary>
    /// <param name="maxAttempts">Max amount of connection attempts</param>
    /// <param name="throwWhenExausted">Throw exception if connection didn't work</param>
    /// <returns>true if connected, otherwise false</returns>
    public async Task<bool> ConnectAsync(int maxAttempts = 0, bool throwWhenExausted = false)
    {
        if (Connection == null) Initialize();

        for (int i = 0; i <= maxAttempts; i++)
        {
            try
            {
                await Connection.Socket.ConnectAsync(_targetEndpoint);
                StartLoop();
                break;
            }
            catch
            {
                if (i == maxAttempts)
                    if (throwWhenExausted)
                        throw;
                    else
                        return false;
            }
        }
        localEndPoint = Connection.Socket.LocalEndPoint as IPEndPoint;
        remoteEndPoint = Connection.Socket.RemoteEndPoint as IPEndPoint;

        while (ConnectionState == ConnectState.PENDING)
            await Task.Delay(10);

        return true;
    }

    private void Initialize()
    {
        Connection = new TcpChannel(new Socket(_targetEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp));

        ConnectionState = ConnectState.PENDING;
        TokenSource = new CancellationTokenSource();
    }

    private void StartLoop()
    {
        _listener = Task.Run(async () =>
        {
            await foreach (var msg in ReceiveMessagesAsync())
            {
                if (msg != null)
                    HandleMessage(msg);
            }
        });
    }
}