namespace Net.Connection.Clients
{
    using Net.Messages;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Cryptography;

    public class ServerClient : GeneralClient
    {
        public delegate void RecieveObject(object Obj, EndPoint Remote);
        public event RecieveObject OnRecieve;

        internal ServerClient(Socket soc, NetSettings settings = default) 
        {
            if (settings == default) settings = new NetSettings();

            this.Settings = settings;
            this.Soc = soc;

            Reciever = Recieve();

            OnRecieveObject = delegate (object o)
            {
                OnRecieve(o, Soc.RemoteEndPoint);
            };

            SendMessage(new SettingsMessage(Settings));
            if (!settings.UseEncryption) return;

            RSAParameters p;
            CryptoServices.GenerateKeyPair(out RSAParameters Public, out p);
            RsaKey = p;

            SendMessage(new EncryptionMessage(EncryptionMessage.Stage.SYN, Public));
        }
    }
}
