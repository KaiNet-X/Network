using Net;
using Net.Connection.Channels;
using Net.Connection.Clients;
using Net.Connection.Servers;
using System.Net;
using System.Text;

var addr = (await Dns.GetHostAddressesAsync(IPAddress.Loopback.ToString()))[0];

UdpChannel chann = null;
var server = new Server(new IPEndPoint(addr, 9090), 1);
server.OnClientChannelOpened += Server_OnClientChannelOpened;
await server.StartAsync();

var client = new Client(new IPEndPoint(addr, 9090));
await client.ConnectAsync();
chann = await client.OpenChannelAsync();
await chann.SendBytesAsync(Encoding.UTF8.GetBytes("Hello from the other side 0"));

async void Server_OnClientChannelOpened(UdpChannel ch, ServerClient arg2)
{
    ulong i = 1;
    while (true)
    {
        var b = await ch.RecieveBytesAsync();
        if (b != null && b.Length > 0)
        {
            Console.WriteLine(Encoding.UTF8.GetString(b));
            await chann.SendBytesAsync(Encoding.UTF8.GetBytes($"Hello from the other side {i}"));
            i++;
        }
    }
}

Console.ReadKey();