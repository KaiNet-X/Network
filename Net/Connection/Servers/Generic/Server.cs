namespace Net.Connection.Servers.Generic;

using Channels;
using Clients.Generic;
using Messages;
using Net;
using Net.Internals;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Default server implementation
/// </summary>
public abstract class Server<ClientType, ConnectionType> : BaseServer<ClientType> where ConnectionType : BaseChannel where ClientType : ObjectClient<ConnectionType>, IServerClient
{
    protected volatile SemaphoreSlim _semaphore = new(1);
    private ConcurrentDictionary<Type, Func<object, ClientType, Task>> ObjectEvents = new();
    protected readonly HashSet<Type> RegisteredObjectTypes;
    protected readonly ConcurrentDictionary<Type, Func<BaseChannel, ClientType, Task>> ChannelEvents = new();

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
    public ConnectionSettings Settings { get; protected set; }

    /// <summary>
    /// Asynchronous handlers for custom message types
    /// </summary>
    protected Dictionary<Type, Func<MessageBase, ClientType, Task>> CustomMessageHandlers = new();

    protected Func<BaseChannel, ClientType, Task> channelOpened;

    protected Func<MessageBase, ClientType, Task> unregisteredMessage;

    /// <summary>
    /// Invoked when a client is connected
    /// </summary>
    public event Action<ClientType> OnClientConnected;

    private Func<DisconnectionInfo, ClientType, Task> clientDisconnected;
    // TODO: Fix order of args
    protected Dictionary<Type, Func<ClientType, Task<BaseChannel>>> OpenChannelMethods = new();
    protected Dictionary<Type, Func<ChannelManagementMessage, ClientType, Task>> ChannelMessages = new();
    protected Dictionary<Type, Func<BaseChannel, ClientType, Task>> CloseChannelMethods = new();

    protected Func<ObjectMessageErrorFrame, ClientType, Task> ObjectError;

    public Server(ConnectionSettings settings)
    {
        Settings = settings;
        if (settings.ServerRequiresRegisteredTypes) 
            RegisteredObjectTypes = new();
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
                c.SetRegisteredObjectTypes(RegisteredObjectTypes);
                c.OnAnyChannel((ch) => channelOpened?.Invoke(ch, c));
                c.OnAnyMessage(m => unregisteredMessage?.Invoke(m, c));
                c.OnObjectError(e => ObjectError?.Invoke(e, c));
                c.OnDisconnected(async g =>
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

                    clientDisconnected?.Invoke(g, c);
                });

                foreach (var v in ObjectEvents)
                    c.RegisterReceive(v.Key, obj => v.Value(obj, c));

                foreach (var v in CustomMessageHandlers)
                    c.OnMessageReceived(v.Key, mb => v.Value(mb, c));

                foreach (var v in ChannelEvents)
                    c.OnChannel(v.Key, ch => v.Value(ch, c));

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

    public void WhitelistObjectType(Type type) =>
        RegisteredObjectTypes.Add(type);

    public void WhitelistObjectType<T>() =>
        RegisteredObjectTypes.Add(typeof(T));

    public void OnObjectError(Func<ObjectMessageErrorFrame, ClientType, Task> handler) =>
        ObjectError = handler;

    public void OnObjectError(Action<ObjectMessageErrorFrame, ClientType> handler) =>
        OnObjectError(Utilities.SyncToAsync(handler));

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

    public void OnDisconnect(Func<DisconnectionInfo, ClientType, Task> onDisconnect) =>
        clientDisconnected = onDisconnect;

    public void OnDisconnect(Action<DisconnectionInfo, ClientType> onDisconnect) =>
        clientDisconnected = (inf, sc) =>
        {
            onDisconnect(inf, sc);
            return Task.CompletedTask;
        };

    public void OnAnyChannel(Func<BaseChannel, ClientType, Task> handler) =>
        channelOpened = handler;

    public void OnAnyChannel(Action<BaseChannel, ClientType> handler) =>
        channelOpened = (bc, sc) =>
        {
            handler(bc, sc);
            return Task.CompletedTask;
        };

    public void OnChannel(Type channelType, Func<BaseChannel, ClientType, Task> onChannelOpened)
    {
        if (!TypeHandler.IsHerritableType<BaseChannel>(channelType))
            throw new ArgumentException($"Expected channel type but got {channelType.Name} instead.");

        ChannelEvents[channelType] = onChannelOpened;
    }

    public void OnChannel(Type channelType, Action<BaseChannel, ClientType> onChannelOpened)
    {
        if (!TypeHandler.IsHerritableType<BaseChannel>(channelType))
            throw new ArgumentException($"Expected channel type but got {channelType.Name} instead.");

        ChannelEvents[channelType] = (bc, sc) =>
        {
            onChannelOpened(bc, sc);
            return Task.CompletedTask;
        };
    }

    public void OnChannel<TChannel>(Func<TChannel, ClientType, Task> onChannelOpened) where TChannel : BaseChannel =>
        OnChannel(typeof(TChannel), (bc, sc) => onChannelOpened(bc as TChannel, sc));

    public void OnChannel<TChannel>(Action<TChannel, ClientType> onChannelOpened) where TChannel : BaseChannel =>
        OnChannel(typeof(TChannel), (bc, sc) => onChannelOpened(bc as TChannel, sc));

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

    public void OnUnregisteredMessage(Func<MessageBase, ClientType, Task> onAnyMessage) =>
        unregisteredMessage = onAnyMessage;

    public void OnUnregisteredMessage(Action<MessageBase, ClientType> onAnyMessage) =>
        OnUnregisteredMessage(Utilities.SyncToAsync(onAnyMessage));

    public void OnMessage<T>(Action<T, ClientType> handler) where T : MessageBase =>
        OnMessage(Utilities.SyncToAsync(handler));

    public void OnMessage(Action<MessageBase, ClientType> handler, Type messageType) =>
        OnMessage(Utilities.SyncToAsync(handler), messageType);

    public void OnMessage<T>(Func<T, ClientType, Task> handler) where T : MessageBase =>
        OnMessage((mb, sc) => handler(mb as T, sc), typeof(T));

    public void OnMessage(Func<MessageBase, ClientType, Task> handler, Type messageType)
    {
        var del = (MessageBase mb, ClientType sc) => { 
            handler(mb, sc); 
            return Task.CompletedTask; 
        };
        CustomMessageHandlers.Add(messageType, del);
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var client in Clients)
                client.OnMessageReceived(messageType, mb => del(mb, client));
        }, _semaphore);
    }

    public bool RegisterReceive<T>(Action<T, ClientType> action) =>
        RegisterReceive(Utilities.SyncToAsync(action));

    public bool RegisterReceive<T>(Func<T, ClientType, Task> func)
    {
        var del = (object obj, ClientType sc) => func((T)obj, sc);
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var client in Clients)
                client.RegisterReceive(typeof(T), obj => del(obj, client));
        }, _semaphore);
        return ObjectEvents.TryAdd(typeof(T), del);
    }

    public void UnregisterReceive<T>()
    {
        Utilities.ConcurrentAccess(() =>
        {
            foreach (var client in Clients)
                client.UnregisterReceive<T>();
        }, _semaphore);
    }

    protected abstract Task<ClientType> InitializeClient();
}