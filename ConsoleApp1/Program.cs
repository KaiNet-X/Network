using Net.Connection.Clients;
using Net.Connection.Servers;
using System;

namespace ConsoleApp1
{
    class Program
    {
        public static Server s;

        static void Main(string[] args)
        {
            s = new Server(System.Net.IPAddress.Loopback, 6969, 1);
            s.OnClientConnected = connected;
            s.StartServer();

            while (true)
            {
                s.SendObjectToAll(Console.ReadLine());
            }
        }

        static void connected(ServerClient c)
        {
            Console.WriteLine("Connected");
            c.OnDisconnect = () => Console.WriteLine("Disconnected");
        }
    }
}