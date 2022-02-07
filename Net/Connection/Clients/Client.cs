namespace Net.Connection.Clients
{
    using System.Collections;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    public class Client : GeneralClient, IConnectable
    {
        public readonly IPAddress Address;
        public readonly uint Port;

        public Client(IPAddress Address, uint Port)
        {
            Soc = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            Soc.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

            this.Address = Address;
            this.Port = Port;

            Thread t = new Thread(() =>
            {
                IEnumerator e = Recieve();
                while (true) e.MoveNext();
            });

            t.IsBackground = true;
            t.Start();
        }

        public void Connect()
        {
            Soc.Connect(new IPEndPoint(Address, (int)Port));
            while (!Connected) ;
        }

        public Task ConnectAsync() => Task.Run(() => Connect());
    }
}