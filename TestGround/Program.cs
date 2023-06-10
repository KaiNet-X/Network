using Net;
using Net.Connection.Channels;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using Net.Messages.Parser;
using Net.Serialization;
using System.Net;
using System.Text;

IPAddress address = (await Dns.GetHostAddressesAsync(IPAddress.Loopback.ToString()))[0];
IPEndPoint endpoint = new IPEndPoint(address, 15555);

DateTime last = DateTime.Now;

//MessageParser.Serializer = new JSerializer();
Server server = new Server(endpoint, 5, new ServerSettings { UseEncryption = true, ConnectionPollTimeout = 10000000 });
server.OnClientConnected += ClientConnected;
server.OnClientObjectReceived += Server_OnClientObjectReceived;

server.Start();

Client client = new Client(endpoint);

client.OnReceiveObject += Client_OnReceiveObject;

client.Connect();

Console.ReadKey();


void ClientConnected(ServerClient client)
{
    last = DateTime.Now;
    client.SendObject(0);
}

void Server_OnClientObjectReceived(object arg1, ServerClient arg2)
{
    Console.WriteLine($"Up: {(DateTime.Now - last).Milliseconds}");
    last = DateTime.Now;
    arg2.SendObject(0);
}

void Client_OnReceiveObject(object obj)
{
    Console.WriteLine($"Down: {(DateTime.Now - last).Milliseconds}");
    last = DateTime.Now;
    client.SendObject(0);
}