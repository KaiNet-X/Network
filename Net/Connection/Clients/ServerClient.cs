namespace Net.Connection.Clients
{
    using Net.Messages;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Security.Cryptography;

    public class ServerClient : GeneralClient
    {
        private IEnumerator<MessageBase> _reciever;
        private Stopwatch _timer = new Stopwatch();
        //private object Lock = "lock";

        internal ServerClient(Socket soc, NetSettings settings = null) 
        {
            if (settings == default) settings = new NetSettings();

            ConnectionState = ConnectState.CONNECTED;

            this.Settings = settings ?? new NetSettings();
            this.Soc = soc;

            _reciever = RecieveMessages().GetEnumerator();

            SendMessage(new SettingsMessage(Settings));
            if (!settings.UseEncryption) return;

            RSAParameters p;
            CryptoServices.GenerateKeyPair(out RSAParameters Public, out p);
            RsaKey = p;

            SendMessage(new EncryptionMessage(Public));
        }

        internal void GetNextMessage()
        {
            //lock (Lock)
            //{
            var msg = _reciever.Current;
            if (msg != null) 
                HandleMessage(msg);
            else
            {
                if (_timer?.ElapsedMilliseconds == 0)
                    _timer.Restart();
                if (_timer.ElapsedMilliseconds >= 1000)
                {
                    _timer.Reset();
                    StartConnectionPoll();
                }
            }
            _reciever.MoveNext();
            //}
        }
    }
}