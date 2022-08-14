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

/// <summary>
/// Default server implementation
/// </summary>
public class Server : BaseServer<ServerClient>
{
    private List<Socket> _bindingSockets;
    private volatile SemaphoreSlim _semaphore;

    /// <summary>
    /// If the server is active or not
    /// </summary>
    public bool Active { get; private set; } = false;
    
    /// <summary>
    /// If the server is listening for connections
    /// </summary>
    public bool Listening { get; private set; } = false;

    /// <summary>
    /// Settings for this server that are set in the constructor
    /// </summary>
    public readonly NetSettings Settings;

    /// <summary>
    /// Max connections at one time
    /// </summary>
    public ushort? MaxClients;

    /// <summary>
    /// Handlers for custom message types
    /// </summary>
    public Dictionary<string, Action<MessageBase, ServerClient>> CustomMessageHandlers = new();

    /// <summary>
    /// All endpoints the server is accepting connections on
    /// </summary>
    public readonly IPEndPoint[] Endpoints;

    /// <summary>
    /// Delay between client updates; highly reduces CPU usage
    /// </summary>
    public ushort LoopDelay = 1;

    /// <summary>
    /// Invoked when a channel is opened on a client
    /// </summary>
    public event Action<IChannel, ServerClient> OnClientChannelOpened;

    /// <summary>
    /// Invoked when a client receives an object
    /// </summary>
    public event Action<object, ServerClient> OnClientObjectReceived;

    /// <summary>
    /// Invoked when a client receives an unregistered custom message
    /// </summary>
    public event Action<MessageBase, ServerClient> OnUnregisteredMessege;

    /// <summary>
    /// Invoked when a client is connected
    /// </summary>
    public event Action<ServerClient> OnClientConnected;

    /// <summary>
    /// Invoked when a client disconnects
    /// </summary>
    public event Action<ServerClient, bool> OnClientDisconnected;

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="address">IP address for the server to bind to</param>
    /// <param name="port">Port for the server to bind to</param>
    /// <param name="maxClients">Max amount of clients</param>
    /// <param name="settings">Settings for connection</param>
    public Server(IPAddress address, int port, ushort? maxClients = null, NetSettings settings = null) : 
        this(new IPEndPoint(address, port), maxClients, settings) { }

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="endpoint">Endpoint for the server to bind to</param>
    /// <param name="maxClients">Max amount of clients</param>
    /// <param name="settings">Settings for connection</param>
    public Server(IPEndPoint endpoint, ushort? maxClients = null, NetSettings settings = null) : 
        this(new List<IPEndPoint> { endpoint}, maxClients, settings) { }

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="endpoints">List of endpoints for the server to bind to</param>
    /// <param name="maxClients">Max amount of clients</param>
    /// <param name="settings">Settings for connection</param>
    public Server(List<IPEndPoint> endpoints, ushort? maxClients = null, NetSettings settings = null)
    {
        MaxClients = maxClients;
        Settings = settings ?? new NetSettings();
        Endpoints = endpoints.ToArray();
        _bindingSockets = new List<Socket>();
        _semaphore = new SemaphoreSlim(1, 1);

        base.Clients = new List<ServerClient>();

        InitializeSockets(Endpoints);
    }

    /// <summary>
    /// Sends an object to all clients
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    public void SendObjectToAll<T>(T obj) =>
        SendMessageToAll(new ObjectMessage(obj));

    /// <summary>
    /// Sends an object to all clients
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
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
                }
            });

        Task.Run(async () =>
        {
            StartListening();
            while (Listening)
            {
                if (MaxClients != null && Clients.Count >= MaxClients)
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
                    }, _semaphore);

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

    public override void ShutDown()
    {
        Stop();
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var c in Clients)
                c.Close();
        }, _semaphore);
    }

    public override async Task ShutDownAsync()
    {
        await StopAsync();
        await Utilities.ConcurrentAccessAsync(async (ct) =>
        {
            foreach (var c in Clients)
                await c.CloseAsync();
        }, _semaphore);
    }

    public override void Stop()
    {
        Utilities.ConcurrentAccess(() =>
        {
            while (_bindingSockets.Count > 0)
            {
                _bindingSockets[0].Close();
                _bindingSockets.RemoveAt(0);
            }
        }, _semaphore);
    }

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

    public override void SendMessageToAll(MessageBase msg)
    {
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var c in Clients)
                c.SendMessage(msg);
        }, _semaphore);
    }

    public override async Task SendMessageToAllAsync(MessageBase msg, CancellationToken token = default)
    {
        await Utilities.ConcurrentAccessAsync(async (ct) =>
        {
            foreach (var c in Clients)
                await c.SendMessageAsync(msg, token);
        }, _semaphore);
    }

    /// <summary>
    /// Registers an object type. This is used as an optimization before the server sends or receives objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void RegisterType<T>() =>
        Utilities.RegisterType(typeof(T));

    private void InitializeSockets(IPEndPoint[] endpoints)
    {
        foreach (IPEndPoint endpoint in endpoints)
        {
            Socket s = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

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