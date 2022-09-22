using Net;
using Net.Connection.Channels;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.Net;

namespace UnitTests;

public class TcpClients
{
    private static int port = 10000;

    [Fact]
    public void Connect()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        var connected = c.Connect();
        server.ShutDown();
        Assert.True(connected);
    }

    [Fact]
    public async Task ConnectAsync()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        var connected = await c.ConnectAsync();
        await server.ShutDownAsync();
        Assert.True(connected);
    }

    [Fact]
    public async Task DisconnectsGracefully()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);

        server.OnClientDisconnected += async (client, graceful) =>
        {
            await server.ShutDownAsync();
            Assert.True(graceful);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.CloseAsync();
    }

    [Fact]
    public async Task SendPrimitive()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        var str = "hello world";

        server.OnClientObjectReceived += async (obj, client) =>
        {
            await server.ShutDownAsync();
            Assert.Equal(str, obj);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.SendObjectAsync(str);
    }

    [Fact]
    public async Task SendComplex()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        var settings = new NetSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false };

        server.OnClientObjectReceived += async (obj, client) =>
        {
            await server.ShutDownAsync();
            Assert.Equal(settings, obj);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.SendObjectAsync(settings);
    }

    [Fact]
    public async Task SendMultiDimensional()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        var arr = new int[,]
        {
            {9, 8, 7},
            {6, 5, 4},
            {3, 2, 1}
        };

        server.OnClientObjectReceived += async (obj, client) =>
        {
            await server.ShutDownAsync();
            Assert.Equal(arr, obj);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.SendObjectAsync(arr);
    }

    [Fact]
    public async Task SendJagged()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        var arr = new int[][]
        {
            new int[] {9, 8, 7},
            new int[] {6, 5, 4, 69, 6, 555},
            new int[] {3, 2, 1}
        };

        server.OnClientObjectReceived += async (obj, client) =>
        {
            await server.ShutDownAsync();
            Assert.Equal(arr, obj);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.SendObjectAsync(arr);
    }

    [Fact]
    public async Task ServerSendComplex()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        var settings = new NetSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        c.OnReceiveObject += async (obj) =>
        {

        };

        await c.ConnectAsync();
        await c.SendObjectAsync(settings);
    }

    [Fact]
    public async Task ClientOpenChannelAsync()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        server.Start();
        bool s = false;

        server.OnClientChannelOpened += (ch, sc) =>
        {
            s = true;
        };

        var c = new Client(IPAddress.Loopback, port++);

        await c.ConnectAsync();
        var ch = await c.OpenChannelAsync<UdpChannel>();

        Assert.True(s && ch is not null);
        await server.ShutDownAsync();
    }

    [Fact]
    public async Task ChannelSendsData()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        server.Start();
        bool s = false;

        TaskCompletionSource tcs = new TaskCompletionSource();

        server.OnClientChannelOpened += async (ch, sc) =>
        {
            if (System.Text.Encoding.UTF8.GetString(await ch.ReceiveBytesAsync()) == "Hello World")
                s = true;
        };

        var c = new Client(IPAddress.Loopback, port++);

        await c.ConnectAsync();
        var ch = await c.OpenChannelAsync<UdpChannel>();
        await ch.SendBytesAsync(System.Text.Encoding.UTF8.GetBytes("Hello World"));
        await Task.Delay(1000);
        Assert.True(s && ch is not null);
    }
}