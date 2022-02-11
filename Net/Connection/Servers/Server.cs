namespace Net.Connection.Servers
{
    using Messages;
    using Clients;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using System;

    public class Server : ServerBase<ServerClient>
    {
        public readonly IPAddress Address;
        public readonly uint Port;
        public volatile ushort MaxClients;

        private Socket ServerSoc;

        public Action<Guid, ServerClient> OnClientChannelOpened;
        public Action<object, ServerClient> OnClientObjectReceived;

        public Server(IPAddress Address, uint Port, ushort MaxClients, NetSettings settings = default)
        {
            //if (settings == default) settings = new NetSettings();

            this.Address = Address; 
            this.Port = Port;
            this.MaxClients = MaxClients;
            this.Settings = settings;

            Clients = new List<ServerClient>();

            //ServerSoc = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            //ServerSoc.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            InitializeSocket();
        }

        public void SendObjectToAll<T>(T obj) =>
            base.SendMessageToAll(new ObjectMessage(obj));

        public override void StartServer()
        {
            if (ServerSoc == null)
                InitializeSocket();

            if (Settings.SingleThreadedServer)
                Task.Run(() =>
                {
                    while (true)
                    {
                        lock (Clients)
                            foreach (ServerClient c in Clients)
                                c.Reciever.MoveNext();
                    }
                });

            Task.Run(() =>
            {
                ServerSoc.Listen();
                ServerClient c;
                while (Clients.Count <= MaxClients)
                {
                    c = new ServerClient(ServerSoc.Accept(), Settings);
                    c.OnChannelOpened = (guid) => OnClientChannelOpened?.Invoke(guid, c);
                    c.OnRecieveObject = (obj) => OnClientObjectReceived?.Invoke(obj, c);

                    lock (Clients)
                        Clients.Add(c);

                    if (!Settings.SingleThreadedServer)
                        Task.Run(() =>
                        {
                            while (true) c.Reciever.MoveNext();
                        });
                    while (!c.Connected) ;

                    //foreach (Type t in Utilities.NameTypeAssociations.Values)
                    //    c.SendMessage(new TypeMessage(t));

                    c.SendMessage(new ConfirmationMessage("done"));
                }
            });
        }

        public override void ShutDown()
        {
            lock (Clients)
                foreach (var c in Clients) c.Close();
        }

        public void RegisterType<T>()
        {
            Utilities.RegisterType(typeof(T));
        }

        private void InitializeSocket()
        {
            if (Address.AddressFamily == AddressFamily.InterNetwork)
                ServerSoc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            else
                ServerSoc = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            ServerSoc.Bind(new IPEndPoint(Address, (int)Port));
        }
    }
}
