using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Net;
using Net.Connection;

namespace Testing
{
    public class Program
    {
        static void Main(string[] args)
        {
            //Server s = new Server(IPAddress.Loopback, 9696, 2, new NetSettings { SingleThreadedServer = false, UseEncryption = true });
            //s.RegisterType<int>();
            //s.RegisterType<Exception>();
            //s.RegisterType<test>();
            //s.StartServer();

            //Client c1 = new Client(IPAddress.Loopback, 9696);
            //c1.OnRecieveObject = rec;
            //c1.Connect();

            //Client c2 = new Client(IPAddress.Loopback, 9696);
            //c2.OnRecieveObject = rec;
            //c2.Connect();

            //s.SendObjectToAll(new Exception("HERRO"));
            //Console.WriteLine(typeof(test).FullName);
            //while (Console.ReadKey().KeyChar != 'e')
            //{
            //    s.SendObjectToAll(test.GetTest());

            //    s.SendObjectToAll(88);
            //}
            Channel rec = new Channel(false, "PP", new IPEndPoint(IPAddress.Any, 9090));

            Channel send = new Channel(true, "[[", new IPEndPoint(IPAddress.Any, 9090));

            Task<List<byte>> t = rec.Listen();
            send.SendData(Encoding.UTF8.GetBytes("WRYYYYYYYYY")).Wait();
            Console.WriteLine(Encoding.UTF8.GetString(t.Result.ToArray()));
            //UDPSocket s = new UDPSocket();
            //s.Server("127.0.0.1", 27000);

            //UDPSocket c = new UDPSocket();
            //c.Client("127.0.0.1", 27000);
            //c.Send("TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!TEST!");

            Console.ReadKey();
        }

        static void rec(object obj)
        {
            Console.WriteLine(JsonSerializer.Serialize(obj));
        }

        public class test
        {
            public string[] arr { get; set; }
            public string name { get; set; } = "peen";

            public test() { }

            public static test GetTest()
            {
                test t = new test();
                t.arr = new string[]
                {
                    "sdaaSdASfag",
                    "ifgsfhasfhrs",
                    "AAsfgegg",
                    "HERRO"
                };

                return t;
            }
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