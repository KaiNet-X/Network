using Net.Connection.Servers;
using System;

namespace ConsoleApp1
{
    class Program
    {
        public static Server s;

        static void Main(string[] args)
        {
            s = new Server(System.Net.IPAddress.Loopback, 5555, 1);
            s.OnClientConnected = (c) => Console.WriteLine("Connected");
            s.StartServer();

            while (true)
            {
                s.SendObjectToAll(Console.ReadLine());
            }
        }
    }
}