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
        static void Main(string[] args)
        {
            Server s = new Server(IPAddress.Loopback, 9696, 2, new NetSettings { SingleThreadedServer = false, UseEncryption = true });
            s.RegisterType<int>();
            s.RegisterType<Exception>();
            s.RegisterType<test>();
            s.StartServer();

            Client c1 = new Client(IPAddress.Loopback, 9696);
            c1.OnRecieveObject = rec;
            c1.Connect();

            s.OnClientChannelOpened = async (Guid id, ServerClient c) =>
            {
                byte[] bytes = await c.RecieveBytesFromChannelAsync(id);
                Console.WriteLine(Encoding.UTF8.GetString(bytes));
                bytes = await c.RecieveBytesFromChannelAsync(id);
                Console.WriteLine(Encoding.UTF8.GetString(bytes));
                bytes = await c.RecieveBytesFromChannelAsync(id);
                Console.WriteLine(Encoding.UTF8.GetString(bytes));
                bytes = await c.RecieveBytesFromChannelAsync(id);
                Console.WriteLine(Encoding.UTF8.GetString(bytes));
            };
            //c2.OnChannelOpened = async (Guid id) =>
            //{
            //    byte[] bytes = await c2.RecieveBytesFromChannelAsync(id);
            //    Console.WriteLine(bytes);
            //};
            var c1c = c1.OpenChannel();
            c1.SendBytesOnChannel(Encoding.UTF8.GetBytes("Hello from outer space"), c1c);
            c1.SendBytesOnChannel(Encoding.UTF8.GetBytes(@"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Pulvinar pellentesque habitant morbi tristique. Rhoncus aenean vel elit scelerisque mauris pellentesque. Ornare arcu odio ut sem nulla pharetra. Id diam maecenas ultricies mi eget mauris pharetra et ultrices. Aliquam eleifend mi in nulla posuere sollicitudin aliquam. Penatibus et magnis dis parturient montes. In fermentum et sollicitudin ac. Cursus sit amet dictum sit amet justo donec enim. Tellus at urna condimentum mattis pellentesque id nibh tortor. Nec tincidunt praesent semper feugiat nibh sed pulvinar proin. Sed vulputate mi sit amet. Id velit ut tortor pretium viverra suspendisse potenti nullam ac. Enim lobortis scelerisque fermentum dui faucibus. Porta lorem mollis aliquam ut porttitor leo.

Euismod elementum nisi quis eleifend.Sollicitudin ac orci phasellus egestas tellus rutrum tellus pellentesque.Arcu bibendum at varius vel pharetra vel turpis.Pulvinar sapien et ligula ullamcorper malesuada.Consectetur purus ut faucibus pulvinar elementum integer enim neque.Enim nec dui nunc mattis enim ut.Porttitor leo a diam sollicitudin.Sodales ut eu sem integer vitae.Condimentum id venenatis a condimentum vitae sapien pellentesque.Nullam non nisi est sit amet facilisis magna etiam tempor.Odio eu feugiat pretium nibh.Felis imperdiet proin fermentum leo vel.Aenean et tortor at risus viverra adipiscing at.Dictum non consectetur a erat nam at lectus urna.Purus faucibus ornare suspendisse sed nisi lacus sed viverra tellus.Ut enim blandit volutpat maecenas volutpat blandit.Tempor orci eu lobortis elementum nibh tellus molestie nunc non.Eget egestas purus viverra accumsan in nisl nisi scelerisque.

Ut venenatis tellus in metus vulputate eu scelerisque.Et pharetra pharetra massa massa ultricies.Urna porttitor rhoncus dolor purus non enim praesent elementum facilisis.Id diam vel quam elementum.Sed arcu non odio euismod lacinia at quis risus.Maecenas sed enim ut sem viverra aliquet eget sit.Enim praesent elementum facilisis leo.Duis ultricies lacus sed turpis tincidunt id aliquet risus feugiat.Nibh tortor id aliquet lectus proin nibh nisl.Purus viverra accumsan in nisl.Integer enim neque volutpat ac tincidunt.

Sed augue lacus viverra vitae congue eu consequat ac felis.Viverra suspendisse potenti nullam ac tortor vitae.Nibh mauris cursus mattis molestie.Ornare massa eget egestas purus viverra.Eget mauris pharetra et ultrices neque ornare aenean euismod elementum.Turpis egestas sed tempus urna et pharetra.Urna cursus eget nunc scelerisque viverra mauris in aliquam.A arcu cursus vitae congue mauris rhoncus aenean vel.Bibendum neque egestas congue quisque egestas diam in arcu cursus.Accumsan sit amet nulla facilisi morbi.Nam aliquam sem et tortor consequat id porta nibh.Blandit cursus risus at ultrices mi tempus imperdiet nulla.

Vehicula ipsum a arcu cursus vitae congue mauris.Sit amet venenatis urna cursus.Sed augue lacus viverra vitae congue eu consequat ac felis.Scelerisque mauris pellentesque pulvinar pellentesque habitant morbi tristique.Pulvinar mattis nunc sed blandit libero.Cursus sit amet dictum sit.Sed arcu non odio euismod.Nunc sed blandit libero volutpat sed.Mi proin sed libero enim sed.Aliquam ut porttitor leo a diam sollicitudin tempor id eu.Dignissim sodales ut eu sem integer vitae justo eget magna.Augue lacus viverra vitae congue."), c1c);
            while (Console.ReadKey().Key != ConsoleKey.E);
            s.ShutDown();
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