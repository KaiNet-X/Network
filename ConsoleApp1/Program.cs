using Net.Connection.Clients;
using Net.Connection.Servers;
using System;

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
                s.SendObjectToAll(Console.ReadLine());
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
                Console.WriteLine(obj);
        }
    }
}