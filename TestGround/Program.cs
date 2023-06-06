using Net.Connection.Channels;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.Diagnostics;
using System.Net;
using System.Text;

IPAddress address = (await Dns.GetHostAddressesAsync(IPAddress.Loopback.ToString()))[0];
IPEndPoint endpoint = new IPEndPoint(address, 15555);

Server server = new Server(endpoint, 5, new ServerSettings { UseEncryption = true});
Client client = new Client(endpoint);

server.OnClientConnected += ClientConnected;
server.OnClientDisconnected += ClientDisconnected;
server.OnClientChannelOpened += ChannelOpened;

server.Start();

client.Connect();

var c = await client.OpenChannelAsync<EncryptedTcpChannel>();

output();
output();

async void ChannelOpened(IChannel channel, ServerClient sc)
{
    Console.WriteLine("Channel opened");
    await channel.SendBytesAsync(Encoding.UTF8.GetBytes("Hello world"));
    await channel.SendBytesAsync(Encoding.UTF8.GetBytes("Hello world"));
}

void ClientDisconnected(ServerClient client, bool graceful)
{
    if (graceful)
        Console.WriteLine($"{client.RemoteEndpoint.Address}:{client.RemoteEndpoint.Port} disconnected gracefully.");
    else
        Console.WriteLine($"{client.RemoteEndpoint.Address}:{client.RemoteEndpoint.Port} lost connection.");
}

void ClientConnected(ServerClient client)
{
    Console.WriteLine($"{client.RemoteEndpoint.Address}:{client.RemoteEndpoint.Port} connected.");
}

async Task output()
{
    var bytes = await c.ReceiveBytesAsync();
    Console.WriteLine(Encoding.UTF8.GetString(bytes));
}