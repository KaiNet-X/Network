namespace Net.Connection.Servers;

using Clients;
using Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class Server : ServerBase<ServerClient>
{
    private List<Socket> _bindingSockets;

    private Socket ServerSoc;
    private volatile SemaphoreSlim _semaphore;
    new public List<ServerClient> Clients => base.Clients;

    public readonly IPAddress Address;
    public readonly int Port;
    public volatile ushort MaxClients;

    public Action<Guid, ServerClient> OnClientChannelOpened;
    public Action<object, ServerClient> OnClientObjectReceived;
    public Action<ServerClient> OnClientConnected;
    public Action<ServerClient> OnClientDisconnected;

    public Server(IPAddress Address, int Port, ushort maxClients, NetSettings settings = default)
    {
        this.Address = Address; 
        this.Port = Port;
        this.MaxClients = maxClients;
        this.Settings = settings ?? new NetSettings();

        base.Clients = new List<ServerClient>();
        _semaphore = new SemaphoreSlim(1, 1);
        InitializeSocket();
    }

    public Server(IPEndPoint endpoint, ushort maxClients, NetSettings settings = default) : 
        this(endpoint.Address, endpoint.Port, maxClients, settings) { }

    public Server(List<IPEndPoint> endpoints, ushort maxClients, NetSettings settings = default)
    {
        this.MaxClients = maxClients;
        this.Settings = settings ?? new NetSettings();
        this._bindingSockets = new List<Socket>();
        this._semaphore = new SemaphoreSlim(1, 1);

        base.Clients = new List<ServerClient>();

        InitializeSockets(endpoints);
    }

    public void SendObjectToAll<T>(T obj) =>
        SendMessageToAll(new ObjectMessage(obj));

    public async Task SendObjectToAllAsync<T>(T obj) =>
        await SendMessageToAllAsync(new ObjectMessage(obj));

    public override void StartServer()
    {
        if (ServerSoc == null)
            InitializeSocket();

        if (Settings?.SingleThreadedServer == true)
            Task.Run(async () =>
            {
                while (true)
                {
                    await Utilities.ConcurrentAccess(async (ct) =>
                    {
                        foreach (ServerClient c in Clients)
                        {
                            if (ct.IsCancellationRequested)
                                return;
                            await c.GetNextMessage();
                        }
                    }, _semaphore);
                    await Task.Delay(10);
                }
            });

        Task.Run(async () =>
        {
            ServerSoc.Listen();
            while (Clients.Count < MaxClients)
            {
                var c = new ServerClient(ServerSoc.Accept(), Settings);
                c.OnChannelOpened += (guid) => OnClientChannelOpened?.Invoke(guid, c);
                c.OnRecieveObject += (obj) => OnClientObjectReceived?.Invoke(obj, c);
                c.OnDisconnect += async () =>
                {
                    await Utilities.ConcurrentAccess((ct) =>
                    {
                        Clients.Remove(c);
                        return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
                    } , _semaphore);

                    OnClientDisconnected?.Invoke(c);
                };
                await Utilities.ConcurrentAccess((ct) =>
                {
                    Clients.Add(c);
                    return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
                }, _semaphore);

                if (Settings?.SingleThreadedServer == false)
                    Task.Run(async () =>
                    {
                        while (c.ConnectionState == ConnectState.CONNECTED)
                            await c.GetNextMessage();
                    });
                while (c.ConnectionState == ConnectState.PENDING) ;

                c.SendMessage(new ConfirmationMessage("done"));
                OnClientConnected?.Invoke(c);
            }
            ServerSoc.Close();
        });
    }

    public override void ShutDown()
    {
        ShutDownAsync().GetAwaiter().GetResult();
    }

    public override async Task ShutDownAsync()
    {
        await Utilities.ConcurrentAccess((ct) =>
        {
            foreach (var c in Clients)
            {
                c.Close();
            }
            return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
        }, _semaphore);
    }

    public void RegisterType<T>() =>
        Utilities.RegisterType(typeof(T));

    public override void SendMessageToAll(MessageBase msg)
    {
        Task.Run(async () => await SendMessageToAllAsync(msg)).GetAwaiter().GetResult();
    }

    public override async Task SendMessageToAllAsync(MessageBase msg)
    {
        await Utilities.ConcurrentAccess((ct) =>
        {
            foreach (var c in Clients)
            {
                if (ct.IsCancellationRequested)
                    return Task.FromCanceled(ct);
                c.SendMessage(msg);
            }
            return Task.CompletedTask;
        }, _semaphore);
    }

    private void InitializeSocket()
    {
        ServerSoc = Address.AddressFamily == AddressFamily.InterNetwork ?
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) :
            new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

        ServerSoc.Bind(new IPEndPoint(Address, (int)Port));
    }

    private void InitializeSockets(List<IPEndPoint> endpoints)
    {
        foreach (IPEndPoint endpoint in endpoints)
        {
            Socket s = endpoint.AddressFamily == AddressFamily.InterNetwork ?
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) :
            new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            s.Bind(endpoint);
            _bindingSockets.Add(s);
        }
    }
}