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
            this.ConnectionState = ConnectState.PENDING;
            this.Address = address;
            InitializeSocket();

            this.Address = address;
            this.Port = port;
        }

        public void Connect()
        {
            Task.Run(async () => await ConnectAsync()).Wait();
        }

        public async Task ConnectAsync()
        {
            if (Soc == null) InitializeSocket();

            Task.Run(() =>
            {
                try
                {
                    foreach (var msg in RecieveMessages())
                        if (msg != null)
                            HandleMessage(msg);
                }
                catch (System.Exception ex)
                {

                }
            });

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