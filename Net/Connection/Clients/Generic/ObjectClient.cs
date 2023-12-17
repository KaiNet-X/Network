namespace Net.Connection.Clients.Generic;

using Channels;
using Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Generic base client for Client and ServerClient that adds functionality for sending/receiving objects.
/// </summary>
public abstract class ObjectClient<MainChannel> : GeneralClient<MainChannel> where MainChannel : class, IChannel
{
    private ConcurrentDictionary<Type, Action<object>> objectEvents = new();
    private ConcurrentDictionary<Type, Func<object, Task>> asyncObjectEvents = new();
    private GuardedChannelList _channelsBack;

    protected ConcurrentDictionary<Type, Func<Task<IChannel>>> OpenChannelMethods = new();
    protected ConcurrentDictionary<Type, Func<ChannelManagementMessage, Task>> ChannelMessages = new();
    protected ConcurrentDictionary<Type, Func<IChannel, Task>> CloseChannelMethods = new();
    protected internal List<IChannel> _channels = new();

    protected ObjectClient()
    {
        _MessageHandlers.Add(typeof(ChannelManagementMessage), (mb) =>
        {
            var m = mb as ChannelManagementMessage;
            if (m.Info is not null && m.Info.ContainsKey("Type"))
            {
                foreach (var name in ChannelMessages.Keys)
                {
                    if (name.Name == m.Info["Type"])
                    {
                        ChannelMessages[name](m);
                        break;
                    }
                }
            }
        });
    }

    /// <summary>
    /// List of active channels associated with this object
    /// </summary>
    public GuardedChannelList Channels => _channelsBack ??= _channels;

    /// <summary>
    /// Invoked when the client receives an object
    /// </summary>
    public event Action<object> OnReceiveObject;

    /// <summary>
    /// Invoked when the client receives an object
    /// </summary>
    public event Func<object, Task> OnReceiveObjectAsync;

    /// <summary>
    /// Invoked when a channel is opened
    /// </summary>
    public Action<IChannel> OnChannelOpened;

    /// <summary>
    /// Invoked when a channel is opened
    /// </summary>
    public Func<IChannel, Task> OnChannelOpenedAsync;

    protected internal async Task ChannelOpenedAsync(IChannel c) 
    {
        _channels.Add(c);
        OnChannelOpened?.Invoke(c);
        if (OnChannelOpenedAsync != null)
            await OnChannelOpenedAsync(c);
    }

    /// <summary>
    /// Registers a generic action to be invoked when an object of specified type is received
    /// </summary>
    /// <typeparam name="T">Type to return</typeparam>
    /// <param name="action"></param>
    /// <returns>False if there is already a handler for type T, otherwise true</returns>
    public bool RegisterReceive<T>(Action<T> action) =>
        RegisterReceive(typeof(T), (obj) => action((T)obj));

    /// <summary>
    /// Registers a generic action to be invoked asynchronously when an object of specified type is received
    /// </summary>
    /// <typeparam name="T">Type to return</typeparam>
    /// <param name="func"></param>
    /// <returns>False if there is already a handler for type T, otherwise true</returns>
    public bool RegisterReceiveAsync<T>(Func<T, Task> func) =>
        RegisterReceiveAsync(typeof(T), (obj) => func((T)obj));

    /// <summary>
    /// Registers a generic action to be invoked when an object of specified type is received
    /// </summary>
    /// <param name="t"></param>
    /// <param name="action"></param>
    /// <returns>False if there is already a handler for type T, otherwise true</returns>
    public bool RegisterReceive(Type t, Action<object> action) =>
        objectEvents.TryAdd(t, action);

    /// <summary>
    /// Registers a generic action to be invoked asynchronously when an object of specified type is received
    /// </summary>
    /// <param name="t"></param>
    /// <param name="func"></param>
    /// <returns>False if there is already a handler for type T, otherwise true</returns>
    public bool RegisterReceiveAsync(Type t, Func<object, Task> func) =>
        asyncObjectEvents.TryAdd(t, func);

    /// <summary>
    /// Unregisters handlers for T
    /// </summary>
    /// <typeparam name="T">Type of the handler</typeparam>
    /// <returns>True if a handler existed, otherwise false</returns>
    public bool UnregisterReceive<T>() =>
        objectEvents.Remove(typeof(T), out _);

    /// <summary>
    /// Unregisters handlers for T
    /// </summary>
    /// <typeparam name="T">Type of the handler</typeparam>
    /// <returns>True if a handler existed, otherwise false</returns>
    public bool UnregisterReceiveAsync<T>() =>
        asyncObjectEvents.Remove(typeof(T), out _);

    /// <summary>
    /// Sends an object to the remote client
    /// </summary>
    /// <typeparam name="T">Type of the object to be sent</typeparam>
    /// <param name="obj">Object</param>
    public virtual void SendObject<T>(T obj) =>
        SendMessage(new ObjectMessage(obj));

    /// <summary>
    /// Sends an object to the remote client
    /// </summary>
    /// <typeparam name="T">Type of the object to be sent</typeparam>
    /// <param name="obj">Object</param>
    /// <param name="token"></param>
    public virtual async Task SendObjectAsync<T>(T obj, CancellationToken token = default) =>
        await SendMessageAsync(new ObjectMessage(obj), token);

    /// <summary>
    /// Closes a channel associated with this client. 
    /// </summary>
    /// <param name="c"></param>
    public void CloseChannel(IChannel c) =>
        CloseChannelAsync(c).GetAwaiter().GetResult();

    /// <summary>
    /// Closes a channel associated with this client. 
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public async Task CloseChannelAsync(IChannel c)
    {
        if (!Channels.Contains(c)) 
            throw new InvalidOperationException("This channel does not belong to this client.");

        await CloseChannelMethods[c.GetType()](c);
        _channels.Remove(c);
    }

    /// <summary>
    /// Opens a new channel of the given type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<T> OpenChannelAsync<T>() where T : class, IChannel
    {
        var c = (await OpenChannelMethods[typeof(T)]()) as T;
        _channels.Add(c);
        return c;
    }

    /// <summary>
    /// Tells the client how to add a channel of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="open">Method to create a new channel and notify the other host.</param>
    /// <param name="channelManagement">Manages the creation of this channel. This can be called multiple times before negotiation is complete and the connection is created.</param>
    /// <param name="close">Specifies how to close the channel.</param>
    public void RegisterChannelType<T>(Func<Task<T>> open, Func<ChannelManagementMessage, Task> channelManagement, Func<T, Task> close) where T : IChannel
    {
        OpenChannelMethods[typeof(T)] = async () => await open();
        ChannelMessages[typeof(T)] = channelManagement;
        CloseChannelMethods[typeof(T)] = async (t) => await close((T)t);
    }

    /// <summary>
    /// This is called whenever a message is recieved. 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    protected override async Task HandleMessageAsync(MessageBase message)
    {
        switch (message)
        {
            case ObjectMessage m:
                HandleObject(m);
                break;
            case DisconnectMessage m:
                HandleDisconnect(m);
                break;
            case ChannelManagementMessage m:
                await ChannelMessages[Utilities.ResolveType(m.Type)](m);
                break;
            default:
                await base.HandleMessageAsync(message);
                break;
        }
    }

    private void HandleObject(MessageBase mb)
    {
        var m = mb as ObjectMessage;
        var obj = m.GetValue();
        var type = obj.GetType();

        var oe = objectEvents.ContainsKey(type);
        var oea = asyncObjectEvents.ContainsKey(type);

        if (oe)
            Task.Run(() => 
            {
                if (objectEvents.TryGetValue(type, out Action<object> a)) a.Invoke(obj);
            });

        if (oea)
            Task.Run(async () =>
            {
                if (asyncObjectEvents.TryGetValue(type, out Func<object, Task> a)) await a.Invoke(obj);
            });

        if (OnReceiveObject is not null && !oe && !oea)
            Task.Run(() => OnReceiveObject(obj));

        if (OnReceiveObjectAsync is not null && !oe && !oea)
            Task.Run(async () => await OnReceiveObjectAsync(obj));
    }

    private void HandleDisconnect(DisconnectMessage m)
    {
        DisconnectedEvent(new DisconnectionInfo { Reason = DisconnectionReason.Closed });
    }

    private protected override void CloseConnection()
    {
        for (int i = 0; i < Channels.Count;)
            CloseChannel(Channels[i]);
    }
}