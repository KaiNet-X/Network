namespace Net.Connection.Clients
{
    using Net.Messages;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Security.Cryptography;

    public class ServerClient : GeneralClient
    {
        private IEnumerator<MessageBase> Reciever;

        internal ServerClient(Socket soc, NetSettings settings = default) 
        {
            if (settings == default) settings = new NetSettings();

            ConnectionState = ConnectState.CONNECTED;

            this.Settings = settings;
            this.Soc = soc;

            Reciever = RecieveMessages().GetEnumerator();

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
            else StartConnectionPoll();
            Reciever.MoveNext();
        }
    }
}