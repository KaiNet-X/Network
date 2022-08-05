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
    }
}