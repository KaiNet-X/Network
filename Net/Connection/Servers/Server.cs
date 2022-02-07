﻿namespace Net.Connection.Servers
{
    using Messages;
    using Clients;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    public class Server : ServerBase<ServerClient>
    {
        public readonly IPAddress Address;
        public readonly uint Port;
        public volatile ushort MaxClients;

        private Socket ServerSoc;

        public Server(IPAddress Address, uint Port, ushort MaxClients, NetSettings settings = default)
        {
            //if (settings == default) settings = new NetSettings();

            this.Address = Address; 
            this.Port = Port;
            this.MaxClients = MaxClients;
            this.Settings = settings;

            Clients = new List<ServerClient>();

            ServerSoc = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            ServerSoc.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            ServerSoc.Bind(new IPEndPoint(Address, (int)Port));
        }

        public void SendObjectToAll<T>(T obj) =>
            base.SendMessageToAll(new ObjectMessage(obj));

        public override void StartServer()
        {
            if (ServerSoc == null)
            {
                ServerSoc = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                ServerSoc.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }

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

                    lock (Clients)
                        Clients.Add(c);

                    if (!Settings.SingleThreadedServer)
                    {
                        Task.Run(() =>
                        {
                            while (true) c.Reciever.MoveNext();
                        });

                    }
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
    }
}
