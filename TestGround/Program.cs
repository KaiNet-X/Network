using Net;
using Net.Connection.Channels;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.Net;
using System.Text;

var strs = new string[] { "hello", "world", "." };
var lambdas = new List<Action>();

foreach (var v in strs)
    lambdas.Add(() => Console.WriteLine(v));

foreach (var v in lambdas) v();

IPAddress address = (await Dns.GetHostAddressesAsync(IPAddress.Loopback.ToString()))[0];
IPEndPoint endpoint = new IPEndPoint(address, 15555);

DateTime last = DateTime.Now;

//MessageParser.Serializer = new JSerializer();
TcpServer server = new TcpServer(endpoint, new ServerSettings { UseEncryption = true, ConnectionPollTimeout = 10000000, MaxClientConnections = 5 });
server.OnClientConnected += ClientConnected;
server.OnClientObjectReceived += Server_OnClientObjectReceived;
server.OnClientChannelOpened += Server_OnClientChannelOpened;

void Server_OnClientChannelOpened(IChannel arg1, ServerClient arg2)
{
    Task.Run(() =>
    {
        while (true)
            arg1.SendBytes("HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH"u8.ToArray());
    });
}

server.Start();

Client client = new Client(endpoint);
//client.OnReceiveObject += Client_OnReceiveObject;

client.RegisterReceive<string>(str =>
{
    Console.WriteLine($"Down: {(DateTime.Now - last).Milliseconds}");
    last = DateTime.Now;
    client.SendObject(str + " Hello world");
});

client.Connect();

var udp = await client.OpenChannelAsync<EncryptedTcpChannel>();

while (true)
    Console.WriteLine(Encoding.UTF8.GetString(await udp.ReceiveBytesAsync()));


void ClientConnected(ServerClient client)
{
    last = DateTime.Now;
    client.SendObject("Hello world");
}

void Server_OnClientObjectReceived(object arg1, ServerClient arg2)
{
    Console.WriteLine($"Up: {(DateTime.Now - last).Milliseconds}");
    Console.WriteLine(arg1);
    last = DateTime.Now;
    arg2.SendObject(arg1);
}