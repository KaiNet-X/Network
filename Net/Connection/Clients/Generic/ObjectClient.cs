namespace Net.Connection.Clients.Generic;

using Channels;
using Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Base client for Client and ServerClient that adds functionality for sending/receiving objects.
/// </summary>
public abstract class ObjectClient<MainChannel> : GeneralClient<MainChannel> where MainChannel : class, IChannel
{
    protected Dictionary<Type, Func<Task<IChannel>>> OpenChannelMethods = new();
    protected Dictionary<Type, Func<ChannelManagementMessage, Task>> ChannelMessages = new();
    protected Dictionary<Type, Func<IChannel, Task>> CloseChannelMethods = new();

    public List<IChannel> Channels = new();
    private Dictionary<Type, IInvokable> objectEvents = new();
    private Dictionary<Type, IAsyncInvokable> asyncObjectEvents = new();
    /// <summary>
    /// Invoked when the client receives an object
    /// </summary>
    public event Action<object> OnReceiveObject;

    /// <summary>
    /// Invoked when a channel is opened
    /// </summary>
    public event Action<IChannel> OnChannelOpened;

    protected List<IChannel> _connectionWait = new();

    protected void ChannelOpened(IChannel c) =>
        OnChannelOpened?.Invoke(c);

    /// <summary>
    /// Registers a generic action to be invoked when an object of specified type is received
    /// </summary>
    /// <typeparam name="T">Type to return</typeparam>
    /// <param name="action"></param>
    /// <returns>False if there is already a handler for type T, otherwise true</returns>
    public bool RegisterReceiveObject<T>(Action<T> action) => 
        objectEvents.TryAdd(typeof(T), new Invokable<T>(action));

    /// <summary>
    /// Registers a generic action to be invoked asynchronously when an object of specified type is received
    /// </summary>
    /// <typeparam name="T">Type to return</typeparam>
    /// <param name="action"></param>
    /// <returns>False if there is already a handler for type T, otherwise true</returns>
    public bool RegisterReceiveObjectAsync<T>(Func<T, Task> action) => 
        asyncObjectEvents.TryAdd(typeof(T), new AsyncInvokable<T>(action));

    /// <summary>
    /// Unregisters handlers for T
    /// </summary>
    /// <typeparam name="T">Type of the handler</typeparam>
    /// <returns>True if a handler existed, otherwise false</returns>
    public bool UnregisterReceiveObject<T>()
    {
        var type = typeof(T);
        if (!objectEvents.ContainsKey(type))
            return false;
        objectEvents.Remove(type);
        return true;
    }

    /// <summary>
    /// Unregisters handlers for T
    /// </summary>
    /// <typeparam name="T">Type of the handler</typeparam>
    /// <returns>True if a handler existed, otherwise false</returns>
    public bool UnregisterReceiveObjectAsync<T>()
    {
        var type = typeof(T);
        if (!asyncObjectEvents.ContainsKey(type))
            return false;
        asyncObjectEvents.Remove(type);
        return true;
    }

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
    public virtual async Task SendObjectAsync<T>(T obj, CancellationToken token = default) =>
        await SendMessageAsync(new ObjectMessage(obj), token);

    public void CloseChannel(IChannel c) =>
        CloseChannelMethods[c.GetType()](c).GetAwaiter().GetResult();

    public async Task CloseChannelAsync(IChannel c) =>
        await CloseChannelMethods[c.GetType()](c);

    public async Task<T> OpenChannelAsync<T>() where T : class, IChannel =>
        (await OpenChannelMethods[typeof(T)]()) as T;

    public void RegisterChannelType<T>(Func<Task<T>> open, Func<ChannelManagementMessage, Task> channelManagement, Func<T, Task> close) where T : IChannel
    {
        OpenChannelMethods[typeof(T)] = async () => await open();
        ChannelMessages[typeof(T)] = channelManagement;
        CloseChannelMethods[typeof(T)] = async (t) => await close((T)t);
    }

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
                ChannelMessages[Utilities.ResolveType(m.Type)](m);
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
        if (objectEvents.ContainsKey(type))
        {
            Task.Run(() => objectEvents[type].Invoke(obj));
            Task.Run(async () => await asyncObjectEvents[type].InvokeAsync(obj));
            return;
        }
        if (OnReceiveObject is not null)
            Task.Run(() => OnReceiveObject(obj));
    }

    private void HandleDisconnect(MessageBase _)
    {
        DisconnectedEvent(true);
    }
}