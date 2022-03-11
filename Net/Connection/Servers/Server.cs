﻿namespace Net.Connection.Servers
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
        public Action<ServerClient> OnClientConnected;

        public Server(IPAddress Address, uint Port, ushort MaxClients, NetSettings settings = default)
        {
            this.Address = Address; 
            this.Port = Port;
            this.MaxClients = MaxClients;
            this.Settings = settings;

            Clients = new List<ServerClient>();

            InitializeSocket();
        }

        public void SendObjectToAll<T>(T obj) =>
            base.SendMessageToAll(new ObjectMessage(obj));

        public override void StartServer()
        {
            if (ServerSoc == null)
                InitializeSocket();

            if (Settings?.SingleThreadedServer == true)
                Task.Run(() =>
                {
                    while (true)
                    {
                        lock (Clients)
                            foreach (ServerClient c in Clients)
                                c.GetNextMessage();
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
                            while (true) 
                                c.GetNextMessage();
                        });
                    while (!c.Connected) ;

                    c.SendMessage(new ConfirmationMessage("done"));
                    OnClientConnected?.Invoke(c);
                }
            });
        }

        public override void ShutDown()
        {
            lock (Clients)
                foreach (var c in Clients)
                    c.Close();
        }

        public void RegisterType<T>() =>
            Utilities.RegisterType(typeof(T));

        private void InitializeSocket()
        {
            ServerSoc = Address.AddressFamily == AddressFamily.InterNetwork ? 
                new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) :
                new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            ServerSoc.Bind(new IPEndPoint(Address, (int)Port));
        }
    }
}