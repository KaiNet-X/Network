namespace Net.Connection.Clients
{
    using Net.Messages;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Security.Cryptography;

    public class ServerClient : GeneralClient
    {
        //public Action<object, EndPoint> OnRecieve;
        private IEnumerator<MessageBase> Reciever;

        internal ServerClient(Socket soc, NetSettings settings = default) 
        {
            if (settings == default) settings = new NetSettings();

            this.Settings = settings;
            this.Soc = soc;

            Reciever = RecieveMessages().GetEnumerator();

            //OnRecieveObject = delegate (object o)
            //{
            //    OnRecieve(o, Soc.RemoteEndPoint);
            //};

            SendMessage(new SettingsMessage(Settings));
            if (!settings.UseEncryption) return;

            RSAParameters p;
            CryptoServices.GenerateKeyPair(out RSAParameters Public, out p);
            RsaKey = p;

            SendMessage(new EncryptionMessage(Public));
        }

        internal void GetNextMessage()
        {
            var msg = Reciever.Current;
            if (msg != null) HandleMessage(msg);
            Reciever.MoveNext();
        }
    }
}
