namespace Net.Connection.Clients;

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

    public bool Connect(int maxAttempts = 0, bool throwWhenExausted = false)
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
                if (i == maxAttempts)
                    if (throwWhenExausted)
                        throw;
                    else
                        return false;
            }
        }
        while (ConnectionState == ConnectState.PENDING) ;
        return true;
    }

    public async Task<bool> ConnectAsync(int maxAttempts = 0, bool throwWhenExausted = false)
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
                if (i == maxAttempts)
                    if (throwWhenExausted)
                        throw;
                    else
                        return false;
            }
        }
        while (ConnectionState == ConnectState.PENDING)
            await Task.Delay(50);

        return true;
    }

    private void Initialize()
    {
        Soc = new Socket(_targetEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        ConnectionState = ConnectState.PENDING;
        TokenSource = new System.Threading.CancellationTokenSource();
    }

    private void StartLoop()
    {
        _listener = Task.Run(async () =>
        {
            await foreach (var msg in ReceiveMessagesAsync())
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