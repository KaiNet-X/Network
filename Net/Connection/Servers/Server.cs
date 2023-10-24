namespace Net.Connection.Servers;

using Channels;
using Clients.Tcp;
using Messages;
using Servers.Generic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    private volatile SemaphoreSlim _semaphore = new(1);

    private ConcurrentDictionary<Type, Func<object, ServerClient, Task>> asyncObjectEvents = new();
    private ConcurrentDictionary<Type, Action<object, ServerClient>> objectEvents = new();

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
    public readonly ServerSettings Settings;

    /// <summary>
    /// Handlers for custom message types
    /// </summary>
    protected Dictionary<Type, Action<MessageBase, ServerClient>> _CustomMessageHandlers = new();

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
    public event Action<ServerClient, DisconnectionInfo> OnClientDisconnected;

    protected Dictionary<Type, Func<ServerClient, Task<IChannel>>> OpenChannelMethods = new();
    protected Dictionary<Type, Func<ChannelManagementMessage, ServerClient, Task>> ChannelMessages = new();
    protected Dictionary<Type, Func<IChannel, ServerClient, Task>> CloseChannelMethods = new();

    /// <summary>
    /// Endpoints passed to the server as arguments
    /// </summary>
    public readonly List<IPEndPoint> Endpoints;

    /// <summary>
    /// Endpoints of all active binding sockets
    /// </summary>
    public List<IPEndPoint> ActiveEndpoints => _bindingSockets.Select(s => (IPEndPoint)s.LocalEndPoint).ToList();


    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="address">IP address for the server to bind to</param>
    /// <param name="port">Port for the server to bind to</param>
    /// <param name="maxClients">Max amount of clients</param>
    /// <param name="settings">Settings for connection</param>
    public Server(IPAddress address, int port, ServerSettings settings = null) : 
        this(new IPEndPoint(address, port), settings) { }

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="endpoint">Endpoint for the server to bind to</param>
    /// <param name="maxClients">Max amount of clients</param>
    /// <param name="settings">Settings for connection</param>
    public Server(IPEndPoint endpoint, ServerSettings settings = null) : 
        this(new List<IPEndPoint> { endpoint }, settings) { }

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="endpoints">List of endpoints for the server to bind to</param>
    /// <param name="maxClients">Max amount of clients</param>
    /// <param name="settings">Settings for connection</param>
    public Server(List<IPEndPoint> endpoints, ServerSettings settings = null)
    {
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
        if (_bindingSockets.Count == 0)
            InitializeSockets(Endpoints);

        StartListening();

        if (Settings.SingleThreadedServer)
            _ = Task.Run(async () =>
            {
                while (Active)
                {
                    await Utilities.ConcurrentAccessAsync(async (ct) =>
                    {
                        foreach (ServerClient c in Clients)
                        {
                            if (ct.IsCancellationRequested || c.ConnectionState == ConnectionState.CLOSED)
                                return;
                            await c.GetNextMessageAsync();
                        }
                    }, _semaphore);
                }
            });

        var tcs = new TaskCompletionSource();

        Active = Listening = true;

        _ = Task.Run(async () =>
        {
            while (Listening)
            {
                if (Settings.MaxClientConnections > 0 && Clients.Count >= Settings.MaxClientConnections)
                {
                    await tcs.Task;
                    Interlocked.Exchange(ref tcs, new TaskCompletionSource());
                }

                var c = new ServerClient(await GetNextConnection(), Settings);

                c.OnChannelOpened += (ch) => OnClientChannelOpened?.Invoke(ch, c);
                c.OnReceiveObject += (obj) => OnClientObjectReceived?.Invoke(obj, c);
                c.OnDisconnect += async (g) =>
                {
                    if (Settings.RemoveClientAfterDisconnect)
                    {
                        await Utilities.ConcurrentAccessAsync((ct) =>
                        {
                            if (Settings.MaxClientConnections > 0 && Clients.Count == Settings.MaxClientConnections)
                                tcs.SetResult();

                            _clients.Remove(c);
                            return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
                        }, _semaphore);
                    }

                    OnClientDisconnected?.Invoke(c, g);
                };

                c.OnUnregisteredMessage += (m) =>
                {
                    OnUnregisteredMessage?.Invoke(m, c);
                };

                foreach (var v in objectEvents)
                    c.RegisterReceiveObject(v.Key, (obj) => v.Value(obj, c));

                foreach (var v in asyncObjectEvents)
                    c.RegisterReceiveObjectAsync(v.Key, (obj) => v.Value(obj, c));

                foreach (var v in _CustomMessageHandlers)
                    c.RegisterMessageHandler(mb => v.Value(mb, c), v.Key);

                await Utilities.ConcurrentAccessAsync((ct) =>
                {
                    _clients.Add(c);
                    return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
                }, _semaphore);

                if (!Settings.SingleThreadedServer)
                    _ = Task.Run(async () =>
                    {
                        while (c.ConnectionState != ConnectionState.CLOSED && Active)
                            await c.GetNextMessageAsync();
                    });

                await c.connectedTask;

                OnClientConnected?.Invoke(c);
            }
            _bindingSockets.ForEach(socket => socket.Close());
        });
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
            _clients.Clear();
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
            _clients.Clear();
        }, _semaphore);
    }

    /// <summary>
    /// Stops listening for new connections but still maintains active ones.
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
    /// Stops listening for new connections but still maintains active ones.
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
    public override void SendMessageToAll(MessageBase msg) =>
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var c in Clients)
                c.SendMessage(msg);
        }, _semaphore);

    /// <summary>
    /// Send a message to all clients
    /// </summary>
    /// <param name="msg">Message to be sent</param>
    public override Task SendMessageToAllAsync(MessageBase msg, CancellationToken token = default) =>
        Utilities.ConcurrentAccessAsync(async (ct) =>
        {
            foreach (var c in Clients)
                await c.SendMessageAsync(msg, token);
        }, _semaphore);

    /// <summary>
    /// Tells clients that connect after this method is called how to add a channel of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="open">Method to create a new channel and notify the other host.</param>
    /// <param name="channelManagement">Manages the creation of this channel. This can be called multiple times before negotiation is complete and the connection is created.</param>
    /// <param name="close">Specifies how to close the channel.</param>
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

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="handler"></param>
    public void RegisterMessageHandler<T>(Action<T, ServerClient> handler) where T : MessageBase =>
        _CustomMessageHandlers.Add(typeof(T), (mb, sc) => handler((T)mb, sc));

    public void RegisterMessageHandler(Action<MessageBase, ServerClient> handler, Type messageType) =>
        _CustomMessageHandlers.Add(messageType, (mb, sc) => handler(mb, sc));

    public bool RegisterReceiveObject<T>(Action<T, ServerClient> action) =>
    objectEvents.TryAdd(typeof(T), (obj, sc) => action((T)obj, sc));

    public bool RegisterReceiveObjectAsync<T>(Func<T, ServerClient, Task> func) =>
        asyncObjectEvents.TryAdd(typeof(T), (obj, sc) => func((T)obj, sc));

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