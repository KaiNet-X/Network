namespace Net.Connection.Servers.Generic;

using Channels;
using Clients.Generic;
using Clients.LegacyTcp;
using Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Default server implementation
/// </summary>
public abstract class Server<ClientType, ConnectionType> : BaseServer<ClientType> where ConnectionType : class, IChannel where ClientType : ObjectClient<ConnectionType>, IServerClient
{
    protected volatile SemaphoreSlim _semaphore = new(1);
    private ConcurrentDictionary<Type, Func<object, ClientType, Task>> asyncObjectEvents = new();
    private ConcurrentDictionary<Type, Action<object, ClientType>> objectEvents = new();

    /// <summary>
    /// If the server is active or not
    /// </summary>
    public bool Active { get; protected set; } = false;

    /// <summary>
    /// If the server is listening for connections
    /// </summary>
    public bool Listening { get; protected set; } = false;

    /// <summary>
    /// Settings for this server
    /// </summary>
    public ServerSettings Settings { get; protected set; }

    /// <summary>
    /// Handlers for custom message types
    /// </summary>
    protected Dictionary<Type, Action<MessageBase, ClientType>> _CustomMessageHandlers = new();

    /// <summary>
    /// Asynchronous handlers for custom message types
    /// </summary>
    protected Dictionary<Type, Func<MessageBase, ClientType, Task>> _AsyncCustomMessageHandlers = new();

    /// <summary>
    /// Invoked when a channel is opened on a client
    /// </summary>
    public event Action<IChannel, ClientType> OnClientChannelOpened;

    /// <summary>
    /// Invoked when a client receives an object
    /// </summary>
    public event Action<object, ClientType> OnClientObjectReceived;

    /// <summary>
    /// Invoked when a client receives an unregistered custom message
    /// </summary>
    public event Action<MessageBase, ClientType> OnUnregisteredMessage;

    /// <summary>
    /// Invoked when a client is connected
    /// </summary>
    public event Action<ClientType> OnClientConnected;

    /// <summary>
    /// Invoked when a client disconnects
    /// </summary>
    public event Action<ClientType, DisconnectionInfo> OnClientDisconnected;

    protected Dictionary<Type, Func<LegacyServerClient, Task<IChannel>>> OpenChannelMethods = new();
    protected Dictionary<Type, Func<ChannelManagementMessage, LegacyServerClient, Task>> ChannelMessages = new();
    protected Dictionary<Type, Func<IChannel, LegacyServerClient, Task>> CloseChannelMethods = new();

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
    /// <param name="token"></param>
    public async Task SendObjectToAllAsync<T>(T obj, CancellationToken token = default) =>
        await SendMessageToAllAsync(new ObjectMessage(obj), token);

    public override void Start()
    {
        if (Settings.SingleThreadedServer)
            _ = Task.Factory.StartNew(async () =>
            {
                while (Active)
                {
                    await Utilities.ConcurrentAccessAsync(async (ct) =>
                    {
                        foreach (ClientType c in Clients)
                        {
                            if (ct.IsCancellationRequested || c.ConnectionState == ConnectionState.CLOSED)
                                return;
                            await c.ReceiveNextAsync();
                        }
                    }, _semaphore);
                }
            }, TaskCreationOptions.LongRunning);

        var tcs = new TaskCompletionSource();

        Active = Listening = true;

        _ = Task.Factory.StartNew(async () =>
        {
            while (Listening)
            {
                if (Settings.MaxClientConnections > 0 && Clients.Count >= Settings.MaxClientConnections)
                {
                    await tcs.Task;
                    Interlocked.Exchange(ref tcs, new TaskCompletionSource());
                }

                var c = await InitializeClient();

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
                    c.RegisterReceive(v.Key, (obj) => v.Value(obj, c));

                foreach (var v in asyncObjectEvents)
                    c.RegisterReceiveAsync(v.Key, (obj) => v.Value(obj, c));

                foreach (var v in _CustomMessageHandlers)
                    c.RegisterMessageHandler(mb => v.Value(mb, c), v.Key);

                await Utilities.ConcurrentAccessAsync((ct) =>
                {
                    _clients.Add(c);
                    return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
                }, _semaphore);

                if (!Settings.SingleThreadedServer)
                    _ = Task.Factory.StartNew(async () =>
                    {
                        while (c.ConnectionState != ConnectionState.CLOSED)
                            await c.ReceiveNextAsync();
                    }, TaskCreationOptions.LongRunning);

                await c.connectedTask;

                OnClientConnected?.Invoke(c);
            }
        }, TaskCreationOptions.LongRunning);
    }

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
    /// Registers an object type. This can be used as an optimization before the server sends or receives objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void RegisterType<T>() =>
        Utilities.RegisterType(typeof(T));

    public void RegisterMessageHandler<T>(Action<T, ClientType> handler) where T : MessageBase 
    {
        var del = (MessageBase mb, ClientType sc) => handler((T)mb, sc);
        _CustomMessageHandlers.Add(typeof(T), del);
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var client in Clients)
                client.RegisterMessageHandler<T>(mb => del(mb, client));
        }, _semaphore);
    }

    public void RegisterMessageHandler(Action<MessageBase, ClientType> handler, Type messageType)
    {
        var del = (MessageBase mb, ClientType sc) => handler(mb, sc);
        _CustomMessageHandlers.Add(messageType, (mb, sc) => handler(mb, sc));
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var client in Clients)
                client.RegisterMessageHandler(mb => del(mb, client), messageType);
        }, _semaphore);
    }

    public void RegisterAsyncMessageHandler<T>(Func<T, ClientType, Task> handler) where T : MessageBase
    {
        var del = (MessageBase mb, ClientType sc) => handler((T)mb, sc);
        _AsyncCustomMessageHandlers.Add(typeof(T), del);
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var client in Clients)
                client.RegisterAsyncMessageHandler<T>(mb => del(mb, client));
        }, _semaphore);
    }

    public void RegisterAsyncMessageHandler(Func<MessageBase, ClientType, Task> handler, Type messageType)
    {
        var del = (MessageBase mb, ClientType sc) => handler(mb, sc);
        _AsyncCustomMessageHandlers.Add(messageType, (mb, sc) => handler(mb, sc));
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var client in Clients)
                client.RegisterAsyncMessageHandler(mb => del(mb, client), messageType);
        }, _semaphore);
    }

    public bool RegisterReceiveObject<T>(Action<T, ClientType> action)
    {
        var del = (object obj, ClientType sc) => action((T)obj, sc);
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var client in Clients)
                client.RegisterReceive(typeof(T), obj => del(obj, client));
        }, _semaphore);
        return objectEvents.TryAdd(typeof(T), del);
    }

    public bool RegisterReceiveObjectAsync<T>(Func<T, ClientType, Task> func)
    {
        var del = (object obj, ClientType sc) => func((T)obj, sc);
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var client in Clients)
                client.RegisterReceiveAsync(typeof(T), obj => del(obj, client));
        }, _semaphore);
        return asyncObjectEvents.TryAdd(typeof(T), del);
    }

    protected abstract Task<ClientType> InitializeClient();
}