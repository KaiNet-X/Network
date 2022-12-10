using Net;
using Net.Connection.Channels;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.Diagnostics;
using System.Net;
using System.Text;

var addr = (await Dns.GetHostAddressesAsync(IPAddress.Loopback.ToString()))[0];

ulong bytes = 0;
bool running = true;

var server = new Server(new IPEndPoint(addr, 9090), 1, new NetSettings { EncryptChannels = false});
var client = new Client(new IPEndPoint(addr, 9090));
client.c
server.OnClientChannelOpened += Server_OnClientChannelOpened;
await server.StartAsync();

await client.ConnectAsync();

IChannel c1 = await client.OpenChannelAsync<UdpChannel>();

var stopwatch = Stopwatch.StartNew();
await c1.SendBytesAsync(Encoding.UTF8.GetBytes("Hello from the other side"));
Console.CancelKeyPress += Cancel;

while (running)
{
    var b = await c1.ReceiveBytesAsync();
    if (b != null && b.Length > 0)
    {
        bytes += (ulong)b.Length;
        Console.WriteLine($"{(c1 as UdpChannel).Remote.Port}: {Encoding.UTF8.GetString(b)}");
    }
    await c1.SendBytesAsync(b);
}

async void Server_OnClientChannelOpened(IChannel ch, ServerClient arg2)
{
    //Task.Run(async () =>
    //{
    //    await (await client.OpenChannelAsync<UdpChannel>()).SendBytesAsync(Encoding.UTF8.GetBytes("Hello from the other side 0"));
    //});
    while (running)
    {
        var b = await ch.ReceiveBytesAsync();
        if (b != null && b.Length > 0)
        {
            bytes += (ulong)b.Length;
            Console.WriteLine($"{(ch as UdpChannel).Remote.Port}: {Encoding.UTF8.GetString(b)}");
        }
        await ch.SendBytesAsync(b);
    }
}

async void Cancel(object? obj, ConsoleCancelEventArgs args)
{
    Console.CancelKeyPress -= Cancel;
    stopwatch.Stop();
    Console.WriteLine($"Average bitrate: {bytes / stopwatch.Elapsed.TotalSeconds / 1000000 * 8} megabit");
    Console.SetOut(null);
    await server.ShutDownAsync();
    running = false;
}