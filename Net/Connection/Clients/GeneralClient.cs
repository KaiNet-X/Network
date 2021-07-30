using Net.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Net.Connection
{
    public class GeneralClient : ClientBase
    {
        protected RSAParameters? RsaKey;
        protected byte[] Key;

        protected Socket Soc;

        public bool Connected { get; private set; }

        private EncryptionMessage.Stage stage = EncryptionMessage.Stage.NONE;

        public delegate void RecievedObject(object Obj);
        public delegate void RecievedFile(NetFile File);

        public RecievedObject OnRecieveObject;
        public RecievedFile OnRecieveFile;

        public void SendObject<T>(T obj)
        {
            ObjectMessage msg = new ObjectMessage(obj);

            SendMessage(msg);
        }

        public async Task SendObjectAsync<T>(T obj)
        {
            ObjectMessage msg = new ObjectMessage(obj);

            await SendMessageAsync(msg);
        }

        public override void Connect()
        {
            throw new NotImplementedException();
        }

        private void HandleMessage(MessageBase message)
        {
            switch (message.MessageType)
            {
                case "confirmation":
                    Connected = true;
                    break;
                case "object":
                    OnRecieveObject?.Invoke((message as ObjectMessage).GetValue());
                    break;
                case "settings":
                    Settings = (message as SettingsMessage).GetValue() as NetSettings;
                    if (!Settings.UseEncryption) SendMessage(new ConfirmationMessage("settings"));
                    break;
                case "encryption":
                    var enc = message as EncryptionMessage;
                    stage = enc.stage;
                    if (stage == EncryptionMessage.Stage.SYN)
                    {
                        RsaKey = (RSAParameters)message.GetValue();
                        Key = CryptoServices.KeyFromHash(CryptoServices.CreateHash(Guid.NewGuid().ToByteArray()));
                        SendMessage(new EncryptionMessage(EncryptionMessage.Stage.ACK, Key));
                    }
                    else if (stage == EncryptionMessage.Stage.ACK)
                    {
                        Key = enc.GetValue() as byte[];
                        SendMessage(new EncryptionMessage(EncryptionMessage.Stage.SYNACK));
                        stage = EncryptionMessage.Stage.SYNACK;
                    }
                    else if (stage == EncryptionMessage.Stage.SYNACK)
                    {
                        SendMessage(new ConfirmationMessage("encryption"));
                    }
                    break;
            }
        }

        public override void SendMessage(MessageBase message)
        {
            List<byte> bytes = message.Serialize();
            if (Settings != null && Settings.UseEncryption)
                switch (stage)
                {
                    case EncryptionMessage.Stage.SYN:
                        bytes = new List<byte>(CryptoServices.EncryptRSA(bytes.ToArray(), RsaKey.Value));
                        bytes = MessageParser.AddTags(bytes);
                        break;
                    case EncryptionMessage.Stage.ACK:
                        bytes = new List<byte>(CryptoServices.EncryptAES(bytes.ToArray(), Key));
                        bytes = MessageParser.AddTags(bytes);
                        break;
                    case EncryptionMessage.Stage.SYNACK:
                        bytes = new List<byte>(CryptoServices.EncryptAES(bytes.ToArray(), Key));
                        bytes = MessageParser.AddTags(bytes);

                        break;
                }

            Soc.Send(bytes.ToArray());
        }

        protected internal override IEnumerator Recieve()
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
                    HandleMessage(msg);
                }
            }
        }
    }
}
