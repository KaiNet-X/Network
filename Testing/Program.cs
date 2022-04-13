using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Net;
using Net.Connection;
using Net.Connection.Channels;
using Net.Connection.Clients;
using Net.Connection.Servers;

namespace Testing
{
    public class Program
    {
        static Client c1 = new Client(IPAddress.Parse("127.0.0.1"), 6969);

        static void Main(string[] args)
        {
            c1.OnRecieveObject += rec;
            c1.OnDisconnect += C1_OnDisconnect;
            c1.OnChannelOpened += C1_OnChannelOpened;
            c1.Connect();
            Console.WriteLine("Connected");
            Console.ReadKey();
            c1.Close();
        }

        private static async void C1_OnChannelOpened(Guid obj)
        {
            var bytes = await c1.RecieveBytesFromChannelAsync(obj);
            Console.WriteLine($"{obj}: {Encoding.UTF8.GetString(bytes)}");
            c1.Channels[obj].Dispose();
            c1.Channels.Remove(obj);
        }

        private static void C1_OnDisconnect()
        {
            Console.WriteLine("Disconnected");
        }

        static void rec(object obj)
        {
            Console.WriteLine(JsonSerializer.Serialize(obj));
            c1.SendObject($"Thanks for {obj}");
        }

        public class UDPSocket
        {
            private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            private const int bufSize = 8 * 1024;
            private State state = new State();
            private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
            private AsyncCallback recv = null;

            public class State
            {
                public byte[] buffer = new byte[bufSize];
            }

            public void Server(string address, int port)
            {
                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
                _socket.Bind(new IPEndPoint(IPAddress.Parse(address), port));
                Receive();
            }

            public void Client(string address, int port)
            {
                _socket.Connect(IPAddress.Parse(address), port);
                Receive();
            }

            public void Send(string text)
            {
                byte[] data = Encoding.ASCII.GetBytes(text);
                _socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
                {
                    State so = (State)ar.AsyncState;
                    int bytes = _socket.EndSend(ar);
                    Console.WriteLine("SEND: {0}, {1}", bytes, text);
                }, state);
            }

            private void Receive()
            {
                _socket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
                {
                    State so = (State)ar.AsyncState;
                    int bytes = _socket.EndReceiveFrom(ar, ref epFrom);
                    _socket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                    Console.WriteLine("RECV: {0}: {1}, {2}", epFrom.ToString(), bytes, Encoding.ASCII.GetString(so.buffer, 0, bytes));
                }, state);
            }
        }
    }
}