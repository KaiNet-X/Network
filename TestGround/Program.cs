using Net;
using Net.Connection.Channels;
using Net.Connection.Clients;
using Net.Connection.Servers;
using System.Diagnostics;
using System.Net;
using System.Text;

var addr = (await Dns.GetHostAddressesAsync(IPAddress.Loopback.ToString()))[0];

ulong bytes = 0;

var server = new Server(new IPEndPoint(addr, 9090), 1, new NetSettings { EncryptChannels = false});
var client = new Client(new IPEndPoint(addr, 9090));

server.OnClientChannelOpened += Server_OnClientChannelOpened;
await server.StartAsync();

await client.ConnectAsync();
var stopwatch = Stopwatch.StartNew();

await (await client.OpenChannelAsync()).SendBytesAsync(Encoding.UTF8.GetBytes("Hello from the other side 0"));

async void Server_OnClientChannelOpened(IChannel ch, ServerClient arg2)
{
    Task.Run(async () =>
    {
        await (await client.OpenChannelAsync()).SendBytesAsync(Encoding.UTF8.GetBytes("Hello from the other side 0"));
    });
    var b = await ch.ReceiveBytesAsync();
    if (b != null && b.Length > 0)
    {
        bytes += (ulong)b.Length;
        Console.WriteLine($"{(ch as UdpChannel).Remote.Port}: {Encoding.UTF8.GetString(b)}");
    }
}

Console.ReadKey();
await server.ShutDownAsync();
stopwatch.Stop();
Console.WriteLine($"Average bitrate: {bytes / stopwatch.Elapsed.TotalSeconds / 1000000 * 8} megabit");