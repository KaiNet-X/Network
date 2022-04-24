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
    public IPEndPoint[] Endpoints { get; private set; } 
    private volatile SemaphoreSlim _semaphore;
    new public List<ServerClient> Clients => base.Clients;

    public volatile ushort MaxClients;

    public Action<Guid, ServerClient> OnClientChannelOpened;
    public Action<object, ServerClient> OnClientObjectReceived;
    public Action<ServerClient> OnClientConnected;
    public Action<ServerClient, bool> OnClientDisconnected;

    public Dictionary<string, Action<MessageBase, ServerClient>> CustomMessageHandlers = new Dictionary<string, Action<MessageBase,ServerClient>>();

    public Server(IPAddress address, int port, ushort maxClients, NetSettings settings = default) : 
        this(new IPEndPoint(address, port), maxClients, settings) { }

    public Server(IPEndPoint endpoint, ushort maxClients, NetSettings settings = default) : 
        this(new List<IPEndPoint> { endpoint}, maxClients, settings) { }

    public Server(List<IPEndPoint> endpoints, ushort maxClients, NetSettings settings = default)
    {
        MaxClients = maxClients;
        Settings = settings ?? new NetSettings();
        Endpoints = endpoints.ToArray();
        _bindingSockets = new List<Socket>();
        _semaphore = new SemaphoreSlim(1, 1);

        base.Clients = new List<ServerClient>();

        InitializeSockets(Endpoints);
    }

    public void SendObjectToAll<T>(T obj) =>
        SendMessageToAll(new ObjectMessage(obj));

    public async Task SendObjectToAllAsync<T>(T obj) =>
        await SendMessageToAllAsync(new ObjectMessage(obj));

    public override void StartServer()
    {
        if (_bindingSockets.Count == 0)
            InitializeSockets(Endpoints);

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
            StartListening();
            while (Clients.Count < MaxClients)
            {
                var c = new ServerClient(await GetNextConnection(), Settings);
                c.CustomMessageHandlers = new ();
                c.OnChannelOpened += (guid) => OnClientChannelOpened?.Invoke(guid, c);
                c.OnRecieveObject += (obj) => OnClientObjectReceived?.Invoke(obj, c);
                c.OnDisconnect += async (g) =>
                {
                    await Utilities.ConcurrentAccess((ct) =>
                    {
                        Clients.Remove(c);
                        return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
                    } , _semaphore);

                    OnClientDisconnected?.Invoke(c, g);
                };
                foreach (var v in CustomMessageHandlers)
                    c.CustomMessageHandlers.Add(v.Key, (msg) => v.Value(msg, c));
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
            _bindingSockets.ForEach(socket => socket.Close());
        });
    }

    public override void ShutDown() =>
        ShutDownAsync().GetAwaiter().GetResult();

    public override async Task ShutDownAsync()
    {
        await Utilities.ConcurrentAccess((ct) =>
        {
            while (_bindingSockets.Count > 0)
            {
                _bindingSockets[0].Close();
                _bindingSockets.Remove(_bindingSockets[0]);
            }
            foreach (var c in Clients)
            {
                c.Close();
            }
            return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
        }, _semaphore);
    }

    public void RegisterType<T>() =>
        Utilities.RegisterType(typeof(T));

    public override void SendMessageToAll(MessageBase msg) =>
        Task.Run(async () => await SendMessageToAllAsync(msg)).GetAwaiter().GetResult();

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

    private void InitializeSockets(IPEndPoint[] endpoints)
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

    private void StartListening()
    {
        foreach (Socket s in _bindingSockets)
            s.Listen();
    }

    private async Task<Socket> GetNextConnection()
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        List<Task<Socket>> tasks = new List<Task<Socket>>();

        foreach(Socket s in _bindingSockets)
        {
            tasks.Add(s.AcceptAsync(cts.Token).AsTask());
        }
        var connection = await await Task.WhenAny(tasks);
        cts.Cancel();

        return connection;
    }
}