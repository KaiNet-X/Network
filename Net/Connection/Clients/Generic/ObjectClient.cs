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
    private Dictionary<Type, Func<IChannel>> OpenChannelMethods;
    private Dictionary<Type, Action<IChannel>> CloseChannelMethods;

    /// <summary>
    /// Invoked when the client receives an object
    /// </summary>
    public event Action<object> OnReceiveObject;

    /// <summary>
    /// Invoked when a channel is opened
    /// </summary>
    public event Action<UdpChannel> OnChannelOpened;
    private List<UdpChannel> _connectionWait = new();

    public new void SendMessage(MessageBase message)
    {
        if (ConnectionState == ConnectState.CONNECTED)
            base.SendMessage(message);
    }

    public new async Task SendMessageAsync(MessageBase message, CancellationToken token = default)
    {
        if (ConnectionState == ConnectState.CONNECTED)
            await base.SendMessageAsync(message, token);
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
        CloseChannelMethods[c.GetType()](c);

    public T OpenChannel<T>() where T : class, IChannel =>
        OpenChannelMethods[typeof(T)]() as T;

    public void RegisterChannelType<T>(Func<T> open) where T : class, IChannel =>
        OpenChannelMethods[typeof(T)] = open;


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