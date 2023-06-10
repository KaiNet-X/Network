namespace Net.Connection.Servers;

using Channels;
using Messages;
using Clients.Tcp;
using Servers.Generic;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

/// <summary>
/// Default server implementation
/// </summary>
public class Server : BaseServer<ServerClient>
{
    private List<Socket> _bindingSockets;
    private volatile SemaphoreSlim _semaphore = new(1);

    /// <summary>
    /// If the server is active or not
    /// </summary>
    public bool Active { get; private set; } = false;
    
    /// <summary>
    /// If the server is listening for connections
    /// </summary>
    public bool Listening { get; private set; } = false;

    /// <summary>
    /// Remove clients from the list after disconnection is invoked
    /// </summary>
    public bool RemoveAfterDisconnect { get; set; } = true;

    /// <summary>
    /// Settings for this server that are set in the constructor
    /// </summary>
    public readonly ServerSettings Settings;

    /// <summary>
    /// Max connections at one time
    /// </summary>
    public ushort? MaxClients;

    /// <summary>
    /// Handlers for custom message types
    /// </summary>
    protected Dictionary<Type, Action<MessageBase, ServerClient>> _CustomMessageHandlers = new();

    /// <summary>
    /// Endpoints passed to the server as arguments
    /// </summary>
    public readonly List<IPEndPoint> Endpoints;

    /// <summary>
    /// Endpoints of all active binding sockets
    /// </summary>
    public List<IPEndPoint> ActiveEndpoints => _bindingSockets.Select(s => (IPEndPoint)s.LocalEndPoint).ToList();
   
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
    public event Action<MessageBase, ServerClient> OnUnregisteredMessage;

    /// <summary>
    /// Invoked when a client is connected
    /// </summary>
    public event Action<ServerClient> OnClientConnected;

    /// <summary>
    /// Invoked when a client disconnects
    /// </summary>
    public event Action<ServerClient, bool> OnClientDisconnected;

    protected Dictionary<Type, Func<ServerClient, Task<IChannel>>> OpenChannelMethods = new();
    protected Dictionary<Type, Func<ChannelManagementMessage, ServerClient, Task>> ChannelMessages = new();
    protected Dictionary<Type, Func<IChannel, ServerClient, Task>> CloseChannelMethods = new();

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="address">IP address for the server to bind to</param>
    /// <param name="port">Port for the server to bind to</param>
    /// <param name="maxClients">Max amount of clients</param>
    /// <param name="settings">Settings for connection</param>
    public Server(IPAddress address, int port, ushort? maxClients = null, ServerSettings settings = null) : 
        this(new IPEndPoint(address, port), maxClients, settings) { }

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="endpoint">Endpoint for the server to bind to</param>
    /// <param name="maxClients">Max amount of clients</param>
    /// <param name="settings">Settings for connection</param>
    public Server(IPEndPoint endpoint, ushort? maxClients = null, ServerSettings settings = null) : 
        this(new List<IPEndPoint> { endpoint }, maxClients, settings) { }

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="endpoints">List of endpoints for the server to bind to</param>
    /// <param name="maxClients">Max amount of clients</param>
    /// <param name="settings">Settings for connection</param>
    public Server(List<IPEndPoint> endpoints, ushort? maxClients = null, ServerSettings settings = null)
    {
        MaxClients = maxClients;
        Settings = settings ?? new ServerSettings();
        Endpoints = endpoints;
        _bindingSockets = new List<Socket>();

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

    /// <summary>
    /// Causes the server to listen for incoming connections. Will accept connections until max clients is reached, but will continue after connections are removed.
    /// </summary>
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

                    if (RemoveAfterDisconnect)
                        await Utilities.ConcurrentAccessAsync((ct) =>
                        {
                            Clients.Remove(c);
                            return Task.CompletedTask;
                        }, _semaphore);
                };

                c.OnUnregisteredMessage += (m) =>
                {
                    OnUnregisteredMessage?.Invoke(m, c);
                };

                foreach (var v in _CustomMessageHandlers)
                    c.RegisterMessageHandler(mb => v.Value(mb, c), v.Key);

                await Utilities.ConcurrentAccessAsync((ct) =>
                {
                    Clients.Add(c);
                    return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
                }, _semaphore);

                if (!Settings.SingleThreadedServer)
                    _ = Task.Run(async () =>
                    {
                        while (c.ConnectionState != ConnectState.CLOSED && Active)
                            await c.GetNextMessageAsync();

                    });

                while (c.ConnectionState == ConnectState.PENDING) ;

                c.SendMessage(new ConfirmationMessage(ConfirmationMessage.Confirmation.RESOLVED));
                OnClientConnected?.Invoke(c);
            }
            _bindingSockets.ForEach(socket => socket.Close());
        });
    }

    /// <summary>
    /// Causes the server to listen for incoming connections. Will accept connections until max clients is reached, but will continue after connections are removed.
    /// </summary>
    public override async Task StartAsync()
    {
        await Task.Run(Start);
    }

    /// <summary>
    /// Calls "Stop" and closes all connections.
    /// </summary>
    public override void ShutDown()
    {
        Active = false;
        Stop();
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var c in Clients)
                c.Close();
            Clients.Clear();
        }, _semaphore);
    }

    /// <summary>
    /// Calls "StopAsync" and closes all connections.
    /// </summary>
    public override async Task ShutDownAsync()
    {
        Active = false;
        await StopAsync();
        await Utilities.ConcurrentAccessAsync(async (ct) =>
        {
            foreach (var c in Clients)
                await c.CloseAsync();
            Clients.Clear();
        }, _semaphore);
    }

    /// <summary>
    /// Stops listening for new connections, but still maintains active ones.
    /// </summary>
    public override void Stop()
    {
        Listening = false;
        Utilities.ConcurrentAccess(() =>
        {
            while (_bindingSockets.Count > 0)
            {
                _bindingSockets[0].Close();
                _bindingSockets.RemoveAt(0);
            }
        }, _semaphore);
    }

    /// <summary>
    /// Stops listening for new connections, but still maintains active ones.
    /// </summary>
    public override async Task StopAsync()
    {
        Listening = false;

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

    /// <summary>
    /// Send a message to all clients
    /// </summary>
    /// <param name="msg">Message to be sent</param>
    public override void SendMessageToAll(MessageBase msg)
    {
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var c in Clients)
                c.SendMessage(msg);
        }, _semaphore);
    }

    /// <summary>
    /// Send a message to all clients
    /// </summary>
    /// <param name="msg">Message to be sent</param>
    public override async Task SendMessageToAllAsync(MessageBase msg, CancellationToken token = default)
    {
        await Utilities.ConcurrentAccessAsync(async (ct) =>
        {
            foreach (var c in Clients)
                await c.SendMessageAsync(msg, token);
        }, _semaphore);
    }

    public void RegisterChannelType<T>(Func<ServerClient, Task<T>> open, Func<ChannelManagementMessage, ServerClient, Task> channelManagement, Func<T, ServerClient, Task> close) where T : IChannel
    {
        OpenChannelMethods[typeof(T)] = async (sc) => await open(sc);
        ChannelMessages[typeof(T)] = channelManagement;
        CloseChannelMethods[typeof(T)] = async (c, sc) => await close((T)c, sc);
    }

    /// <summary>
    /// Registers an object type. This is used as an optimization before the server sends or receives objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void RegisterType<T>() =>
        Utilities.RegisterType(typeof(T));

    public void RegisterMessageHandler<T>(Action<T, ServerClient> handler) where T : MessageBase =>
    _CustomMessageHandlers.Add(typeof(T), (mb, sc) => handler((T)mb, sc));

    public void RegisterMessageHandler(Action<MessageBase, ServerClient> handler, Type messageType) =>
        _CustomMessageHandlers.Add(messageType, (mb, sc) => handler(mb, sc));

    private void InitializeSockets(List<IPEndPoint> endpoints)
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