﻿using Net.Connection.Clients;
using Net.Connection.Servers;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TestServer;

class Program
{
    public static Server s;
    public static object o = 1;

    static async Task Main(string[] args)
    {
        s = new Server(IPAddress.Parse("192.168.0.10"), 6969, 3, new Net.NetSettings { UseEncryption = true, ConnectionPollTimeout = 5000});
        s.OnClientConnected = connected;
        s.OnClientObjectReceived += recieved;
        s.StartServer();

        while (true)
        {
            var l = Console.ReadLine();
            if (l.ToLowerInvariant().StartsWith("send channel"))
                await s.Clients[0].SendBytesOnChannelAsync(Encoding.UTF8.GetBytes(l.Substring(13)), await s.Clients[0].OpenChannelAsync());
            else if (l == "EXIT") s.ShutDown();
            else
                s.SendObjectToAll(l);
        }
    }

    static void connected(ServerClient c)
    {
        Console.WriteLine("Connected");
        c.OnDisconnect += () => Console.WriteLine("Disconnected");
    }

    static async void recieved(object obj, ServerClient c)
    {
        foreach (GeneralClient b in s.Clients)
            if (b != c)
                await b.SendObjectAsync(obj);
    }
}
