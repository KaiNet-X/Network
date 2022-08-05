namespace Tests;

using Net;

public class ClientTests
{
    private static int port = 10000;

    [SetUp]
    public void Setup()
    {

    }

    [Test]
    public void Connect()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        var connected = c.Connect();
        server.ShutDown();
        Assert.IsTrue(connected);
    }

    [Test]
    public async Task ConnectAsync()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        var connected = await c.ConnectAsync();
        await server.ShutDownAsync();
        Assert.IsTrue(connected);
    }

    [Test]
    public async Task DisconnectsGracefully()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);

        server.OnClientDisconnected += async (client, graceful) =>
        {
            await server.ShutDownAsync();
            Assert.IsTrue(graceful);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.CloseAsync();
    }

    [Test]
    public async Task SendPrimitive()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        var str = "hello world";

        server.OnClientObjectReceived += async (obj, client) =>
        {
            await server.ShutDownAsync();
            Assert.AreEqual(str, obj);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.SendObjectAsync(str);
    }

    [Test]
    public async Task SendComplex()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        var settings = new NetSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false};

        server.OnClientObjectReceived += async (obj, client) =>
        {
            await server.ShutDownAsync();
            Assert.AreEqual(settings, obj);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.SendObjectAsync(settings);
    }

    [Test]
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
            Assert.AreEqual(arr, obj);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.SendObjectAsync(arr);
    }

    [Test]
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
            Assert.AreEqual(arr, obj);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.SendObjectAsync(arr);
    }
}