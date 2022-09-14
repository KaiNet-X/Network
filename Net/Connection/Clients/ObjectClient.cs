namespace Net.Connection.Clients;

using Net.Connection.Channels;
using Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Base client for Client and ServerClient that adds functionality for sending/receiving objects.
/// </summary>
public class ObjectClient : GeneralSocketClient
{
    /// <summary>
    /// Invoked when the client receives an object
    /// </summary>
    public event Action<object> OnReceiveObject;

    /// <summary>
    /// Invoked when a channel is opened
    /// </summary>
    public event Action<UdpChannel> OnChannelOpened;
    private List<UdpChannel> _connectionWait = new ();

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

    public override void CloseChannel(IChannel c)
    {
        var udp = c as UdpChannel;

        SendMessage(new ChannelManagementMessage(udp.Remote.Port, ChannelManagementMessage.Mode.Close));
        Channels.Remove(c);
        c.Close();
    }

    public override async Task CloseChannelAsync(IChannel c, CancellationToken token = default)
    {
        var udp = c as UdpChannel;

        await SendMessageAsync(new ChannelManagementMessage(udp.Remote.Port, ChannelManagementMessage.Mode.Close), token);
        Channels.Remove(c);
        c.Close();
    }

    public override UdpChannel OpenChannel()
    {
        var key = Settings.EncryptChannels ? CryptoServices.KeyFromHash(CryptoServices.CreateHash(Guid.NewGuid().ToByteArray())) : null;

        UdpChannel c = Settings.EncryptChannels ?
            new(new IPEndPoint(LocalEndpoint.Address, 0), key) :
            new(new IPEndPoint(LocalEndpoint.Address, 0));

        ChannelManagementMessage m = Settings.EncryptChannels ?
            new ChannelManagementMessage(c.Local.Port, ChannelManagementMessage.Mode.Create, key) :
            new ChannelManagementMessage(c.Local.Port, ChannelManagementMessage.Mode.Create);

        _connectionWait.Add(c);

        SendMessage(m);

        while (_connectionWait.Contains(c))
            Thread.Sleep(10);

        Channels.Add(c);

        return c;
    }

    public override async Task<IChannel> OpenChannelAsync(CancellationToken token = default)
    {
        var key = Settings.EncryptChannels ? CryptoServices.KeyFromHash(CryptoServices.CreateHash(Guid.NewGuid().ToByteArray())) : null;

        UdpChannel c = Settings.EncryptChannels ?
            new(new IPEndPoint(LocalEndpoint.Address, 0), key) :
            new(new IPEndPoint(LocalEndpoint.Address, 0));

        ChannelManagementMessage m = Settings.EncryptChannels ?
            new ChannelManagementMessage(c.Local.Port, ChannelManagementMessage.Mode.Create, key) :
            new ChannelManagementMessage(c.Local.Port, ChannelManagementMessage.Mode.Create);

        _connectionWait.Add(c);

        await SendMessageAsync(m, token);

        while (_connectionWait.Contains(c))
            await Task.Delay(10);

        Channels.Add(c);

        return c;
    }

    protected override void HandleMessage(MessageBase message)
    {
        switch (message)
        {
            case ChannelManagementMessage m:
                HandleChannelManagement(m);
                break;
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

    private void HandleChannelManagement(MessageBase mb)
    {
        var m = mb as ChannelManagementMessage;
        if (m.ManageMode == ChannelManagementMessage.Mode.Create)
        {
            var remoteEndpoint = new IPEndPoint(RemoteEndpoint.Address, m.Port);
            UdpChannel c = Settings.UseEncryption ?
                new (new IPEndPoint(LocalEndpoint.Address, 0), m.Aes) :
                new (new IPEndPoint(LocalEndpoint.Address, 0));

            c.SetRemote(remoteEndpoint);
            SendMessage(new ChannelManagementMessage(c.Local.Port, ChannelManagementMessage.Mode.Confirm, m.Port));
            Channels.Add(c);
            if (OnChannelOpened != null)
                Task.Run(() => OnChannelOpened(c));
        }
        else if (m.ManageMode == ChannelManagementMessage.Mode.Confirm)
        {
            var c = _connectionWait.First(c => c.Local.Port == m.IdPort);
            c.SetRemote(new IPEndPoint(LocalEndpoint.Address, m.Port));
            _connectionWait.Remove(c);
        }
        else if (m.ManageMode == ChannelManagementMessage.Mode.Close)
        {
            var c = Channels.First(ch => (ch as UdpChannel).Local.Port == m.Port) as UdpChannel;
            c.Close();
            Channels.Remove(c);
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