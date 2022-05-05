namespace Net.Connection.Clients;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class Client : ObjectClient
{
    private Stopwatch _timer = new Stopwatch();

    public ushort LoopDelay = 10;
    private readonly IPEndPoint _targetEndpoint;

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

        StartLoop();

        for (int i = 0; i <= maxAttempts; i++)
        {
            try
            {
                Soc.Connect(_targetEndpoint);
                break;
            }
            catch
            {
                if (i == maxAttempts && throwWhenExausted)
                    throw;
            }
        }
    }

    public async Task ConnectAsync(int maxAttempts = 0, bool throwWhenExausted = false)
    {
        if (Soc == null) Initialize();

        StartLoop();

        for (int i = 0; i <= maxAttempts; i++)
        {
            try
            {
                await Soc.ConnectAsync(_targetEndpoint);
                break;
            }
            catch
            {
                if (i == maxAttempts && throwWhenExausted) 
                    throw;
            }
        }
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
        Task.Run(async () =>
        {
            foreach (var msg in RecieveMessages())
            {
                if (msg != null)
                    await HandleMessage(msg);
                else
                {
                    if (_timer == null) _timer = Stopwatch.StartNew();
                    else if (_timer?.ElapsedMilliseconds == 0)
                        _timer.Restart();
                    else if (_timer.ElapsedMilliseconds >= 1000)
                    {
                        _timer.Reset();
                        StartConnectionPoll();
                    }
                }
                await Task.Delay(LoopDelay);
            }
        });
    }
}