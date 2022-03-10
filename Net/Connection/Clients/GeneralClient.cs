namespace Net.Connection.Clients
{
    using Messages;
    using Net.Connection.Channels;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    public abstract class GeneralClient : ClientBase, IClosable
    {
        protected RSAParameters? RsaKey;
        protected volatile byte[] Key;

        protected Socket Soc;

        public volatile List<Channel> Channels = new List<Channel>();

        public bool Connected { get; private set; }

        private EncryptionMessage.Stage stage = EncryptionMessage.Stage.NONE;

        public Action<object> OnRecieveObject;
        //public Action<NetFile> OnRecieveFile;
        public Action<Guid> OnChannelOpened;
        public Action<MessageBase> OnRecievedCustomMessage;

        public void SendObject<T>(T obj) =>
            SendMessage(new ObjectMessage(obj));
       
        public async Task SendObjectAsync<T>(T obj) =>
            await SendMessageAsync(new ObjectMessage(obj));

        public Guid OpenChannel()
        {
            var localEP = (Soc.LocalEndPoint as IPEndPoint);
            Channel c = new Channel(localEP.Address);
            Channels.Add(c);
            SendMessage(new ChannelManagementMessage(c.Id, c.Port, ChannelManagementMessage.Mode.Create));
            return c.Id;
        }

        public async Task<Guid> OpenChannelAsync()
        {
            Channel c = new Channel((Soc.LocalEndPoint as IPEndPoint).Address);
            Channels.Add(c);
            await SendMessageAsync(new ChannelManagementMessage(c.Id, c.Port, ChannelManagementMessage.Mode.Create));
            return c.Id;
        }

        public void SendBytesOnChannel(byte[] bytes, Guid id) =>
            Channels.First(c => c.Id == id).SendBytes(bytes);

        public async Task SendBytesOnChannelAsync(byte[] bytes, Guid id) =>
            await Channels.First(c => c.Id == id).SendBytesAsync(bytes);

        public byte[] RecieveBytesFromChannel(Guid id) =>
            Channels.First(c => c.Id == id).RecieveBytes();

        public async Task<byte[]> RecieveBytesFromChannelAsync(Guid id) =>
            await Channels.First(c => c.Id == id).RecieveBytesAsync();

        protected void HandleMessage(MessageBase message)
        {
            switch (message)
            {
                case ConfirmationMessage m:
                    Connected = true;
                    break;
                case ObjectMessage m:
                    OnRecieveObject?.Invoke(m.GetValue());
                    break;
                case SettingsMessage m:
                    Settings = m.GetValue() as NetSettings;
                    if (!Settings.UseEncryption) SendMessage(new ConfirmationMessage("settings"));
                    break;
                case EncryptionMessage m:
                    stage = m.stage;
                    if (stage == EncryptionMessage.Stage.SYN)
                    {
                        RsaKey = (RSAParameters)m.GetValue();
                        Key = CryptoServices.KeyFromHash(CryptoServices.CreateHash(Guid.NewGuid().ToByteArray()));
                        SendMessage(new EncryptionMessage(Key));
                    }
                    else if (stage == EncryptionMessage.Stage.ACK)
                    {
                        Key = m.GetValue() as byte[];
                        SendMessage(new EncryptionMessage(EncryptionMessage.Stage.SYNACK));
                        stage = EncryptionMessage.Stage.SYNACK;
                    }
                    else if (stage == EncryptionMessage.Stage.SYNACK)
                        SendMessage(new ConfirmationMessage("encryption"));
                    break;
                case ChannelManagementMessage m:
                    var val = (Guid)m.GetValue();
                    if (m.ManageMode == ChannelManagementMessage.Mode.Create)
                    {
                        var ipAddr = (Soc.LocalEndPoint as IPEndPoint).Address;
                        var remoteEndpoint = new IPEndPoint((Soc.RemoteEndPoint as IPEndPoint).Address, m.Port);
                        var c = new Channel(ipAddr, remoteEndpoint, val);
                        c.Connected = true;
                        Channels.Add(c);
                        OnChannelOpened?.Invoke(val);
                        SendMessage(new ChannelManagementMessage(val, c.Port, ChannelManagementMessage.Mode.Confirm));
                    }
                    else if (m.ManageMode == ChannelManagementMessage.Mode.Confirm)
                    {
                        var c = Channels.First(c => c.Id == val);
                        c.SetRemote(new IPEndPoint((Soc.RemoteEndPoint as IPEndPoint).Address, m.Port));
                        c.Connected = true;
                    }
                    break;
                default:
                    ;
                    break;
            }
        }

        public override void SendMessage(MessageBase message)
        {
            List<byte> bytes = message.Serialize();
            if (Settings != null && Settings.UseEncryption)
                bytes = stage switch
                {
                    EncryptionMessage.Stage.SYN => new List<byte>(CryptoServices.EncryptRSA(bytes.ToArray(), RsaKey.Value)),
                    EncryptionMessage.Stage.ACK => new List<byte>(CryptoServices.EncryptAES(bytes.ToArray(), Key)),
                    EncryptionMessage.Stage.SYNACK => new List<byte>(CryptoServices.EncryptAES(bytes.ToArray(), Key)),
                    EncryptionMessage.Stage.NONE => bytes
                };

            Soc.Send(MessageParser.AddTags(bytes).ToArray());
        }

        //protected internal override IEnumerator Recieve()
        //{
        //    List<byte> allBytes = new List<byte>();
        //    byte[] buffer;

        //    while (true)
        //    {
        //        int available = Soc.Available;
        //        if (available == 0)
        //        {
        //            yield return null;
        //            continue;
        //        }

        //        buffer = new byte[available];
        //        Task.Delay(10).Wait();
        //        Soc.Receive(buffer);

        //        allBytes.AddRange(buffer);
        //        List<MessageBase> messages = null;

        //        if (Settings != null && Settings.UseEncryption)
        //            switch (stage)
        //            {
        //                case EncryptionMessage.Stage.SYN:
        //                    messages = MessageParser.GetMessagesAes(ref allBytes, Key);
        //                    break;
        //                case EncryptionMessage.Stage.ACK:
        //                    messages = MessageParser.GetMessagesRsa(ref allBytes, RsaKey.Value);
        //                    break;
        //                case EncryptionMessage.Stage.SYNACK:
        //                    messages = MessageParser.GetMessagesAes(ref allBytes, Key);
        //                    break;
        //                default:
        //                    if (RsaKey == null)
        //                        messages = MessageParser.GetMessages(ref allBytes);
        //                    else
        //                        messages = MessageParser.GetMessagesRsa(ref allBytes, RsaKey.Value);
        //                    break;
        //            }
        //        else messages = MessageParser.GetMessages(ref allBytes);
        //        foreach (MessageBase msg in messages)
        //        {
        //            HandleMessage(msg);
        //        }
        //    }
        //}

        protected override IEnumerable<MessageBase> RecieveMessages()
        {
            List<byte> allBytes = new List<byte>();
            byte[] buffer;

            while (true)
            {
                int available = Soc.Available;
                if (available == 0)
                {
                    yield return null;
                    continue;
                }

                buffer = new byte[available];
                Task.Delay(10).Wait();
                Soc.Receive(buffer);

                allBytes.AddRange(buffer);
                List<MessageBase> messages = null;

                if (Settings != null && Settings.UseEncryption)
                    switch (stage)
                    {
                        case EncryptionMessage.Stage.SYN:
                            messages = MessageParser.GetMessagesAes(ref allBytes, Key);
                            break;
                        case EncryptionMessage.Stage.ACK:
                            messages = MessageParser.GetMessagesRsa(ref allBytes, RsaKey.Value);
                            break;
                        case EncryptionMessage.Stage.SYNACK:
                            messages = MessageParser.GetMessagesAes(ref allBytes, Key);
                            break;
                        default:
                            if (RsaKey == null)
                                messages = MessageParser.GetMessages(ref allBytes);
                            else
                                messages = MessageParser.GetMessagesRsa(ref allBytes, RsaKey.Value);
                            break;
                    }
                else messages = MessageParser.GetMessages(ref allBytes);

                foreach (MessageBase msg in messages)
                {
                    yield return msg;
                }
            }
        }

        public void Close()
        {
            Soc.Close();
            Channels.ForEach(c => c.Dispose());
        }

        public Task CloseAsync()
        {
            throw new NotImplementedException();
        }
    }
}