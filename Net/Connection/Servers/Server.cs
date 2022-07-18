namespace Net.Connection.Servers;

using Clients;
using Messages;
using Channels;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class Server : BaseServer<ServerClient>
{
    private List<Socket> _bindingSockets;
    private volatile SemaphoreSlim _semaphore;

    public bool Active { get; private set; } = false;
    public bool Listening { get; private set; } = false;

    public readonly NetSettings Settings;
    public volatile ushort MaxClients;

    public event Action<IChannel, ServerClient> OnClientChannelOpened;
    public event Action<object, ServerClient> OnClientObjectReceived;
    public event Action<MessageBase, ServerClient> OnUnregisteredMessege;
    public event Action<ServerClient> OnClientConnected;
    public event Action<ServerClient, bool> OnClientDisconnected;

    public Dictionary<string, Action<MessageBase, ServerClient>> CustomMessageHandlers = new();

    public ushort LoopDelay = 10;

    public readonly IPEndPoint[] Endpoints;

    public Server(IPAddress address, int port, ushort maxClients, NetSettings settings = null) : 
        this(new IPEndPoint(address, port), maxClients, settings) { }

    public Server(IPEndPoint endpoint, ushort maxClients, NetSettings settings = null) : 
        this(new List<IPEndPoint> { endpoint}, maxClients, settings) { }

    public Server(List<IPEndPoint> endpoints, ushort maxClients, NetSettings settings = null)
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

    public async Task SendObjectToAllAsync<T>(T obj, CancellationToken token = default) =>
        await SendMessageToAllAsync(new ObjectMessage(obj), token);

    public override void Start()
    {
        Active = Listening = true;

        if (_bindingSockets.Count == 0)
            InitializeSockets(Endpoints);

        if (Settings.SingleThreadedServer)
            Task.Run(async () =>
            {
                while (Active)
                {
                    await Utilities.ConcurrentAccessAsync(async (ct) =>
                    {
                        foreach (ServerClient c in Clients)
                        {
                            if (ct.IsCancellationRequested || c.ConnectionState == ConnectState.CLOSED)
                                return;
                            await c.GetNextMessageAsync();
                        }
                    }, _semaphore);
                    await Task.Delay(LoopDelay);
                }
            });

        Task.Run(async () =>
        {
            StartListening();
            while (Listening)
            {
                if (Clients.Count >= MaxClients)
                {
                    await Task.Delay(LoopDelay);
                    continue;
                }

                var c = new ServerClient(await GetNextConnection(), Settings);

                c.OnChannelOpened += (ch) => OnClientChannelOpened?.Invoke(ch, c);
                c.OnReceiveObject += (obj) => OnClientObjectReceived?.Invoke(obj, c);
                c.OnDisconnect += async (g) =>
                {
                    await Utilities.ConcurrentAccessAsync((ct) =>
                    {
                        Clients.Remove(c);
                        return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
                    } , _semaphore);

                    OnClientDisconnected?.Invoke(c, g);
                };
                c.OnUnregisteredMessage += (m) =>
                {
                    OnUnregisteredMessege?.Invoke(m, c);
                };

                foreach (var v in CustomMessageHandlers)
                    c.CustomMessageHandlers.Add(v.Key, (msg) => v.Value(msg, c));

                await Utilities.ConcurrentAccessAsync((ct) =>
                {
                    Clients.Add(c);
                    return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
                }, _semaphore);

                if (!Settings.SingleThreadedServer)
                    Task.Run(async () =>
                    {
                        while (c.ConnectionState != ConnectState.CLOSED && Active)
                        {
                            await c.GetNextMessageAsync();
                            await Task.Delay(LoopDelay);
                        }
                    });
                while (c.ConnectionState == ConnectState.PENDING) ;

                c.SendMessage(new ConfirmationMessage(ConfirmationMessage.Confirmation.RESOLVED));
                OnClientConnected?.Invoke(c);
            }
            _bindingSockets.ForEach(socket => socket.Close());
        });
    }

    public override async Task StartAsync()
    {
        await Task.Run(Start);
    }

    public override void ShutDown() =>
        ShutDownAsync().GetAwaiter().GetResult();

    public override async Task ShutDownAsync()
    {
        await StopAsync();
        await Utilities.ConcurrentAccessAsync((ct) =>
        {
                       foreach (var c in Clients)
            {
                c.Close();
            }
            return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
        }, _semaphore);
    }

    public override void Stop() =>
        StopAsync().GetAwaiter().GetResult();

    public override async Task StopAsync()
    {
        await Utilities.ConcurrentAccessAsync((ct) =>
        {
            while (_bindingSockets.Count > 0)
            {
                _bindingSockets[0].Close();
                _bindingSockets.RemoveAt(0);
            }
            return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
        }, _semaphore);
    }

    public override void SendMessageToAll(MessageBase msg) =>
        Task.Run(async () => await SendMessageToAllAsync(msg)).GetAwaiter().GetResult();

    public override async Task SendMessageToAllAsync(MessageBase msg, CancellationToken token = default)
    {
        await Utilities.ConcurrentAccessAsync(async (ct) =>
        {
            foreach (var c in Clients)
                await c.SendMessageAsync(msg, token);
        }, _semaphore);
    }

    public void RegisterType<T>() =>
        Utilities.RegisterType(typeof(T));

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