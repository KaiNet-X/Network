﻿namespace Net.Connection.Clients.Generic;

using Channels;
using Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Generic base client for Client and ServerClient that adds functionality for sending/receiving objects.
/// </summary>
public abstract class ObjectClient<MainChannel> : GeneralClient<MainChannel> where MainChannel : class, IChannel
{
    private Dictionary<Type, IInvokable> objectEvents = new();
    private Dictionary<Type, IAsyncInvokable> asyncObjectEvents = new();
    private GuardedList<IChannel> _channelsBack;

    protected Dictionary<Type, Func<Task<IChannel>>> OpenChannelMethods = new();
    protected Dictionary<Type, Func<ChannelManagementMessage, Task>> ChannelMessages = new();
    protected Dictionary<Type, Func<IChannel, Task>> CloseChannelMethods = new();
    protected List<IChannel> _channels = new();

    /// <summary>
    /// List of active channels associated with this object
    /// </summary>
    public GuardedList<IChannel> Channels => _channelsBack ??= _channels;

    /// <summary>
    /// Invoked when the client receives an object
    /// </summary>
    public event Action<object> OnReceiveObject;

    /// <summary>
    /// Invoked when a channel is opened
    /// </summary>
    public event Action<IChannel> OnChannelOpened;

    protected void ChannelOpened(IChannel c) 
    {
        _channels.Add(c);
        OnChannelOpened?.Invoke(c);
    }

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

        if (objectEvents.ContainsKey(type))
            Task.Run(() => objectEvents[type].Invoke(obj));

        if (asyncObjectEvents.ContainsKey(type))
            Task.Run(async () => await asyncObjectEvents[type].InvokeAsync(obj));

        if (OnReceiveObject is not null)
            Task.Run(() => OnReceiveObject(obj));
    }

    private void HandleDisconnect(MessageBase _)
    {
        DisconnectedEvent(new DisconnectionInfo { Reason = "Remote host disconnected." });
    }
}