namespace Net.Connection.Clients.Generic;

using Channels;
using Messages;
using Net.Internals;
using Net.Messages.Parsing;
using Net.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Generic base client for Client and ServerClient that adds functionality for sending/receiving objects.
/// </summary>
public abstract class ObjectClient<MainChannel> : GeneralClient<MainChannel> where MainChannel : BaseChannel
{
    private GuardedChannelList _channelsBack;

    protected readonly ConcurrentDictionary<Type, Func<object, Task>> ObjectEvents = new();
    protected readonly ConcurrentDictionary<Type, Func<Task<BaseChannel>>> OpenChannelMethods = new();
    protected readonly ConcurrentDictionary<Type, Func<ChannelManagementMessage, Task>> ChannelMessages = new();
    protected readonly ConcurrentDictionary<Type, Func<BaseChannel, Task>> CloseChannelMethods = new();
    protected readonly ConcurrentDictionary<Type, Func<BaseChannel, Task>> ChannelEvents = new();
    protected internal readonly List<BaseChannel> _channels = new();
    protected HashSet<Type> WhitelistedObjectTypes = new();

    protected Func<ObjectMessageErrorFrame, Task> ObjectError;

    /// <summary>
    /// Data serializer used in the default message parser and for deserializing object messages.
    /// </summary>
    public ISerializer Serializer { get; init; } = Consts.DefaultSerializer;

    /// <summary>
    /// List of active channels associated with this object
    /// </summary>
    public GuardedChannelList Channels => _channelsBack ??= _channels;

    protected Func<BaseChannel, Task> channelOpened;

    protected ObjectClient()
    {
        MessageParser ??= new MessageParser(_crypto, Serializer);

        OnMessageReceived(typeof(ChannelManagementMessage), (mb) =>
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

    protected internal async Task ChannelOpenedAsync(BaseChannel c) 
    {
        _channels.Add(c);
        if (ChannelEvents.TryGetValue(c.GetType(), out var handler))
            await handler(c);
        else
            await channelOpened?.Invoke(c);
    }

    public void OnObjectError(Func<ObjectMessageErrorFrame, Task> handler) =>
        ObjectError = handler;

    public void OnObjectError(Action<ObjectMessageErrorFrame> handler) =>
        OnObjectError(Utilities.SyncToAsync(handler));

    public void OnAnyChannel(Func<BaseChannel, Task> handler) =>
        channelOpened = handler;

    public void OnAnyChannel(Action<BaseChannel> handler) =>
        OnAnyChannel(Utilities.SyncToAsync(handler));

    public void OnChannel(Type channelType, Func<BaseChannel, Task> onChannelOpened)
    {
        if (!TypeHandler.IsHerritableType<BaseChannel>(channelType))
            throw new ArgumentException($"Expected channel type but got {channelType.Name} instead.");

        ChannelEvents[channelType] = onChannelOpened;
    }

    public void OnChannel(Type channelType, Action<BaseChannel> onChannelOpened) => 
        OnChannel(channelType, Utilities.SyncToAsync(onChannelOpened));

    public void OnChannel<TChannel>(Func<TChannel, Task> onChannelOpened) where TChannel : BaseChannel =>
        OnChannel(typeof(TChannel), bc => onChannelOpened(bc as TChannel));

    public void OnChannel<TChannel>(Action<TChannel> onChannelOpened) where TChannel : BaseChannel =>
        OnChannel(Utilities.SyncToAsync(onChannelOpened));

    public bool OnReceive(Type type, Action<object> receive) =>
        OnReceive(type, Utilities.SyncToAsync(receive));

    /// <summary>
    /// Registers a generic action to be invoked when an object of specified type is received
    /// </summary>
    /// <typeparam name="T">Type to return</typeparam>
    /// <param name="action"></param>
    /// <returns>False if there is already a handler for type T, otherwise true</returns>
    public bool OnReceive<T>(Action<T> action) =>
        OnReceive(Utilities.SyncToAsync(action));

    public bool OnReceive(Type type, Func<object, Task> receive)
    {
        WhitelistedObjectTypes?.Add(type);
        return ObjectEvents.TryAdd(type, receive);
    }

    /// <summary>
    /// Registers a generic action to be invoked asynchronously when an object of specified type is received
    /// </summary>
    /// <typeparam name="T">Type to return</typeparam>
    /// <param name="func"></param>
    /// <returns>False if there is already a handler for type T, otherwise true</returns>
    public bool OnReceive<T>(Func<T, Task> func) =>
        OnReceive(typeof(T), (obj) => func((T)obj));

    /// <summary>
    /// Unregisters handlers for T
    /// </summary>
    /// <typeparam name="T">Type of the handler</typeparam>
    /// <returns>True if a handler existed, otherwise false</returns>
    public bool UnregisterReceive<T>() =>
        ObjectEvents.Remove(typeof(T), out _);

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
    public void CloseChannel(BaseChannel c) =>
        CloseChannelAsync(c).GetAwaiter().GetResult();

    /// <summary>
    /// Closes a channel associated with this client. 
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public async Task CloseChannelAsync(BaseChannel c)
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
    public async Task<T> OpenChannelAsync<T>() where T : BaseChannel
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
    public void RegisterChannelType<T>(Func<Task<T>> open, Func<ChannelManagementMessage, Task> channelManagement, Func<T, Task> close) where T : BaseChannel
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
                if (TypeHandler.TryGetTypeFromName(m.Type, out Type t))
                    await ChannelMessages[t](m);
                break;
            default:
                await base.HandleMessageAsync(message);
                break;
        }
    }

    private void HandleObject(ObjectMessage m)
    {
        var unknown = !TypeHandler.TryGetTypeFromName(m.TypeName, out Type t);
        var notRegistered = Settings.RequiresWhitelistedTypes && !WhitelistedObjectTypes.Contains(t);

        if (notRegistered || unknown)
        {
            var errorFrame = new ObjectMessageErrorFrame(
                m.Data,
                m.TypeName,
                unknown ?
                    ObjectMessageErrorFrame.UnregisteredTypeReason.TypeUnknown :
                    ObjectMessageErrorFrame.UnregisteredTypeReason.TypeUnregistered,
                Serializer);

            if (ObjectError != null)
                Task.Run(async () =>
                {
                    var task = ObjectError(errorFrame);
                    if (task != null) await task;
                });

            return;
        }

        var obj = Serializer.Deserialize(m.Data, t);
        var type = obj.GetType();

        if (ObjectEvents.TryGetValue(type, out Func<object, Task> a))
            Task.Run(async () =>
            {
                await a(obj);
            });
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