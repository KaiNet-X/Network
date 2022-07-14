namespace Net.Connection.Clients;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class Client : ObjectClient
{
    public ushort LoopDelay = 10;
    private readonly IPEndPoint _targetEndpoint;

    public Task _listener { get; private set; }

    public Client(IPAddress address, int port) : this (new IPEndPoint(address, port)) { }

    public Client(string address, int port) : this(IPAddress.Parse(address), port) { }

    public Client(IPEndPoint ep)
    {
        ConnectionState = ConnectState.PENDING;
        _targetEndpoint = ep;
        Initialize();
    }

    public void Connect(int maxAttempts = 0, bool throwWhenExausted = false)
    {
        if (Soc == null) Initialize();

        for (int i = 0; i <= maxAttempts; i++)
        {
            try
            {
                Soc.Connect(_targetEndpoint);
                StartLoop();
                break;
            }
            catch
            {
                if (i == maxAttempts && throwWhenExausted)
                    throw;
            }
        }
        while (ConnectionState == ConnectState.PENDING) ;
    }

    public async Task ConnectAsync(int maxAttempts = 0, bool throwWhenExausted = false)
    {
        if (Soc == null) Initialize();

        for (int i = 0; i <= maxAttempts; i++)
        {
            try
            {
                await Soc.ConnectAsync(_targetEndpoint);
                StartLoop();
                break;
            }
            catch
            {
                if (i == maxAttempts && throwWhenExausted) 
                    throw;
            }
        }
        while (ConnectionState == ConnectState.PENDING)
            await Task.Delay(50);
    }

    private void Initialize()
    {
        Soc = _targetEndpoint.AddressFamily == AddressFamily.InterNetwork ?
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) :
            new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

        ConnectionState = ConnectState.PENDING;
        TokenSource = new System.Threading.CancellationTokenSource();
    }

    private void StartLoop()
    {
        _listener = Task.Run(async () =>
        {
            foreach (var msg in ReceiveMessages())
            {
                if (msg != null)
                    HandleMessage(msg);
                else if (!AwaitingPoll)
                    StartConnectionPoll();

                await Task.Delay(LoopDelay);
            }
        });
    }
}