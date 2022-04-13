using Net.Connection.Clients;
using Net.Connection.Servers;
using System;
using System.Text;

namespace ConsoleApp1
{
    class Program
    {
        public static Server s;
        public static object o = 1;

        static void Main(string[] args)
        {
            s = new Server(System.Net.IPAddress.Loopback, 6969, 3);
            s.OnClientConnected = connected;
            s.OnClientObjectReceived += recieved;
            s.StartServer();

            while (true)
            {
                var l = Console.ReadLine();
                if (l.ToLowerInvariant().StartsWith("send channel"))
                    s.Clients[0].SendBytesOnChannel(Encoding.UTF8.GetBytes(l.Substring(13)), s.Clients[0].OpenChannel());
                else
                    s.SendObjectToAll(l);
            }
        }

        static void connected(ServerClient c)
        {
            Console.WriteLine("Connected");
            c.OnDisconnect += () => Console.WriteLine("Disconnected");
        }

        static void recieved(object obj, ServerClient c)
        {
            lock (o)
                Console.WriteLine($"{c.Endpoint}: {obj}");
        }
    }
}