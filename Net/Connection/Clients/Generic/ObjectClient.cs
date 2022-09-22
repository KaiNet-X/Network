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
public class ObjectClient<MainChannel> : GeneralClient<MainChannel> where MainChannel : class, IChannel
{
    public Dictionary<Type, Func<Task<IChannel>>> OpenChannelMethods = new();
    public Dictionary<Type, Action<ChannelManagementMessage>> ChannelMessages = new();
    public Dictionary<Type, Action<IChannel>> CloseChannelMethods = new();

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
        CloseChannelMethods[c.GetType()](c);

    public async Task<T> OpenChannelAsync<T>() where T : class, IChannel =>
        (await OpenChannelMethods[typeof(T)]()) as T;

    public void RegisterChannelType<T>(Func<Task<T>> open) where T : class, IChannel =>
         OpenChannelMethods[typeof(T)] = open as Func<Task<IChannel>>;

    protected override void HandleMessage(MessageBase message)
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
                base.HandleMessage(message);
                break;
        }
    }

    private void HandleObject(MessageBase mb)
    {
        var m = mb as ObjectMessage;

        if (OnReceiveObject is not null)
            _invokationList.AddAction(() => OnReceiveObject(m.GetValue()));
    }

    private void HandleDisconnect(MessageBase _)
    {
        DisconnectedEvent(true);
    }
}