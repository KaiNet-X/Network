using Net.Connection.Channels;
using Net.Messages;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Connection.Clients
{
    public class ObjectClient : GeneralClient<Channel>
    {
        public event Action<object> OnReceiveObject;
        public event Action<Channel> OnChannelOpened;

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

        public override Channel OpenChannel()
        {
            Channel c = new Channel((Soc.LocalEndPoint as IPEndPoint).Address) { AesKey = Key };
            Channels.Add(c.Id, c);
            SendMessage(new ChannelManagementMessage(c.Id, c.Port, ChannelManagementMessage.Mode.Create));
            return c;
        }

        public override async Task<Channel> OpenChannelAsync(CancellationToken token = default)
        {
            Channel c = new Channel((Soc.LocalEndPoint as IPEndPoint).Address) { AesKey = Key };
            Channels.Add(c.Id, c);
            await SendMessageAsync(new ChannelManagementMessage(c.Id, c.Port, ChannelManagementMessage.Mode.Create));
            return c;
        }

        public void SendBytesOnChannel(byte[] bytes, Guid id) =>
            Channels[id].SendBytes(bytes);

        public async Task SendBytesOnChannelAsync(byte[] bytes, Guid id, CancellationToken token = default) =>
            await Channels[id].SendBytesAsync(bytes, token);

        public byte[] RecieveBytesFromChannel(Guid id) =>
            Channels[id].RecieveBytes();

        public async Task<byte[]> RecieveBytesFromChannelAsync(Guid id, CancellationToken token = default) =>
            await Channels[id].RecieveBytesAsync(token);

        private async void HandleConnectionPoll(MessageBase mb)
        {
            var m = mb as ConnectionPollMessage;
            switch (m.PollState)
            {
                case ConnectionPollMessage.PollMessage.SYN:
                    SendMessage(new ConnectionPollMessage { PollState = ConnectionPollMessage.PollMessage.ACK });
                    break;
                case ConnectionPollMessage.PollMessage.ACK:
                    PollConnected();
                    break;
                case ConnectionPollMessage.PollMessage.DISCONNECT:
                    await DisconnectedEvent(true);
                    break;
            }
        }

        private void HandleChannelManagement(MessageBase mb)
        {
            var m = mb as ChannelManagementMessage;

            var val = (Guid)m.GetValue();
            if (m.ManageMode == ChannelManagementMessage.Mode.Create)
            {
                var ipAddr = (Soc.LocalEndPoint as IPEndPoint).Address;
                var remoteEndpoint = new IPEndPoint((Soc.RemoteEndPoint as IPEndPoint).Address, m.Port);
                var c = new Channel(ipAddr, remoteEndpoint, val) { AesKey = Key };
                c.Connected = true;
                Channels.Add(c.Id, c);
                SendMessage(new ChannelManagementMessage(val, c.Port, ChannelManagementMessage.Mode.Confirm));
                Task.Run(() => OnChannelOpened?.Invoke(c));
            }
            else if (m.ManageMode == ChannelManagementMessage.Mode.Confirm)
            {
                var c = Channels[val];
                c.SetRemote(new IPEndPoint((Soc.RemoteEndPoint as IPEndPoint).Address, m.Port));
                c.Connected = true;
            }
        }

        private void HandleObject(MessageBase mb)
        {
            var m = mb as ObjectMessage;

            Task.Run(() => OnReceiveObject?.Invoke(m.GetValue()));
        }
    }
}