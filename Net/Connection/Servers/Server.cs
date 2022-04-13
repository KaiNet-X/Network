﻿namespace Net.Connection.Servers
{
    using Messages;
    using Clients;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using System;
    using System.Threading;

    public class Server : ServerBase<ServerClient>
    {
        private Socket ServerSoc;
        private volatile SemaphoreSlim _semaphore;

        public readonly IPAddress Address;
        public readonly uint Port;
        public volatile ushort MaxClients;

        public Action<Guid, ServerClient> OnClientChannelOpened;
        public Action<object, ServerClient> OnClientObjectReceived;
        public Action<ServerClient> OnClientConnected;
        public Action<ServerClient> OnClientDisconnected;

        public Server(IPAddress Address, uint Port, ushort MaxClients, NetSettings settings = default)
        {
            this.Address = Address; 
            this.Port = Port;
            this.MaxClients = MaxClients;
            this.Settings = settings ?? new NetSettings();

            Clients = new List<ServerClient>();
            _semaphore = new SemaphoreSlim(1, 1);
            InitializeSocket();
        }

        public void SendObjectToAll<T>(T obj) =>
            SendMessageToAll(new ObjectMessage(obj));

        public override void StartServer()
        {
            if (ServerSoc == null)
                InitializeSocket();

            if (Settings?.SingleThreadedServer == true)
                Task.Run(async () =>
                {
                    while (true)
                    {
                        await Utilities.ConcurrentAccess(() =>
                        {
                            foreach (ServerClient c in Clients)
                            {
                                c.GetNextMessage();
                            }
                            return Task.CompletedTask;
                        }, _semaphore);
                        await Task.Delay(10);
                    }
                });

            Task.Run(async () =>
            {
                ServerSoc.Listen();
                while (Clients.Count < MaxClients)
                {
                    var c = new ServerClient(ServerSoc.Accept(), Settings);
                    c.OnChannelOpened += (guid) => OnClientChannelOpened?.Invoke(guid, c);
                    c.OnRecieveObject += (obj) => OnClientObjectReceived?.Invoke(obj, c);
                    c.OnDisconnect += async () =>
                    {
                        await Utilities.ConcurrentAccess(() =>
                        {
                            Clients.Remove(c);
                            return Task.CompletedTask;
                        } , _semaphore);

                        OnClientDisconnected?.Invoke(c);
                    };
                    await Utilities.ConcurrentAccess(() =>
                    {
                        Clients.Add(c);
                        return Task.CompletedTask;
                    }, _semaphore);

                    if (Settings?.SingleThreadedServer == false)
                        Task.Run(() =>
                        {
                            while (true) 
                                c.GetNextMessage();
                        });
                    while (c.ConnectionState == ConnectState.PENDING) ;

                    c.SendMessage(new ConfirmationMessage("done"));
                    OnClientConnected?.Invoke(c);
                }
                ServerSoc.Close();
            });
        }

        public override void ShutDown()
        {
            Utilities.ConcurrentAccess(() =>
            {
                foreach (var c in Clients)
                    c.Close();
                return Task.CompletedTask;
            }, _semaphore).Wait();
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

        public override void SendMessageToAll(MessageBase msg)
        {
            Task.Run(async () => await SendMessageToAllAsync(msg)).Wait();
        }

        public override async Task SendMessageToAllAsync(MessageBase msg)
        {
            await Utilities.ConcurrentAccess(() =>
            {
                foreach (var c in Clients)
                    c.SendMessage(msg);
                return Task.CompletedTask;
            }, _semaphore);
        }
    }
}