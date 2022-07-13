namespace Net.Connection.Clients;

using Net.Connection.Channels;
using Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

public class ObjectClient : GeneralClient<UdpChannel>
{
    public event Action<object> OnReceiveObject;
    public event Action<UdpChannel> OnChannelOpened;
    private List<UdpChannel> _connectionWait = new ();

    public ObjectClient()
    {
        CustomMessageHandlers.Add(nameof(ConnectionPollMessage), HandleConnectionPoll);
        CustomMessageHandlers.Add(nameof(ChannelManagementMessage), HandleChannelManagement);
        CustomMessageHandlers.Add(nameof(ObjectMessage), HandleObject);
    }

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

    public virtual void SendObject<T>(T obj) =>
        SendMessage(new ObjectMessage(obj));

    public virtual async Task SendObjectAsync<T>(T obj, CancellationToken token = default) =>
        await SendMessageAsync(new ObjectMessage(obj), token);

    public override void CloseChannel(UdpChannel c)
    {
        SendMessage(new ChannelManagementMessage(c.Remote.Port, ChannelManagementMessage.Mode.Close));
        Channels.Remove(c);
        c.Dispose();
    }

    public override async Task CloseChannelAsync(UdpChannel c, CancellationToken token = default)
    {
        await SendMessageAsync(new ChannelManagementMessage(c.Remote.Port, ChannelManagementMessage.Mode.Close), token);
        Channels.Remove(c);
        c.Dispose();
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

    public override async Task<UdpChannel> OpenChannelAsync(CancellationToken token = default)
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

    private async void HandleConnectionPoll(MessageBase mb)
    {
        var m = mb as ConnectionPollMessage;
        switch (m.PollState)
        {
            case ConnectionPollMessage.PollMessage.SYN:
                SendMessage(new ConnectionPollMessage { PollState = ConnectionPollMessage.PollMessage.ACK });
                break;
            case ConnectionPollMessage.PollMessage.ACK:
                OnPollConnected();
                break;
            case ConnectionPollMessage.PollMessage.DISCONNECT:
                await DisconnectedEvent(true);
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
            Channels.Add(c);
            SendMessage(new ChannelManagementMessage(c.Local.Port, ChannelManagementMessage.Mode.Confirm, m.Port));
            Task.Run(() => OnChannelOpened?.Invoke(c));
        }
        else if (m.ManageMode == ChannelManagementMessage.Mode.Confirm)
        {
            var c = _connectionWait.First(c => c.Local.Port == m.IdPort);
            c.SetRemote(new IPEndPoint(LocalEndpoint.Address, m.Port));
            _connectionWait.Remove(c);
        }
        else if (m.ManageMode == ChannelManagementMessage.Mode.Close)
        {
            var c = Channels.First(ch => ch.Local.Port == m.Port);
            Channels.Remove(c);
        }
    }

    private void HandleObject(MessageBase mb)
    {
        var m = mb as ObjectMessage;

        Task.Run(() => OnReceiveObject?.Invoke(m.GetValue()));
    }
}