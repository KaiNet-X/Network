namespace Net.Connection.Clients
{
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    public class Client : GeneralClient, IConnectable
    {
        public readonly IPAddress Address;
        public readonly uint Port;

        public Client(IPAddress address, uint port)
        {
            this.Address = address;
            InitializeSocket();

            this.Address = address;
            this.Port = port;

            Task.Run(() =>
            {
                foreach (var msg in RecieveMessages())
                    if (msg != null) 
                        HandleMessage(msg);
            });
        }

        public void Connect()
        {
            if (Soc == null) InitializeSocket();

            Soc.Connect(new IPEndPoint(Address, (int)Port));
            while (!Soc.Connected) ;
            ConnectionState = ConnectState.CONNECTED;
        }

        public async Task ConnectAsync()
        {
            if (Soc == null) InitializeSocket();

            await Soc.ConnectAsync(new IPEndPoint(Address, (int)Port));
            ConnectionState = ConnectState.CONNECTED;
        }

        private void InitializeSocket()
        {
            if (Address.AddressFamily == AddressFamily.InterNetwork)
                Soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            else
                Soc = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
        }
    }
}