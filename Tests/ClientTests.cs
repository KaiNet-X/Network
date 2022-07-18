namespace Tests;

using Net;

public class ClientTests
{
    [SetUp]
    public void Setup()
    {

    }

    [Test]
    public void Connect()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, 10000), 1);
        server.Start();

        var c = new Client(IPAddress.Loopback, 10000);
        Assert.IsTrue(c.Connect());
        server.ShutDown();
    }

    [Test]
    public async Task ConnectAsync()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, 10000), 1);
        server.Start();

        var c = new Client(IPAddress.Loopback, 10000);
        Assert.IsTrue(await c.ConnectAsync());
        await server.ShutDownAsync();
    }

    [Test]
    public async Task SendPrimitive()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, 10000), 1);
        var str = "hello world";

        server.OnClientObjectReceived += async (obj, client) =>
        {
            await server.ShutDownAsync();
            Assert.AreEqual(str, obj);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, 10000);
        await c.ConnectAsync();
    }

    [Test]
    public async Task SendComplex()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, 10000), 1);
        var settings = new NetSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false};

        server.OnClientObjectReceived += async (obj, client) =>
        {
            await server.ShutDownAsync();
            Assert.AreEqual(settings, obj);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, 10000);
        await c.ConnectAsync();
    }

    [Test]
    public async Task DisconnectsGracefully()
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, 10000), 1);
        var settings = new NetSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false };

        server.OnClientDisconnected += async (client, graceful) =>
        {
            await server.ShutDownAsync();
            Assert.IsTrue(graceful);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, 10000);
        await c.ConnectAsync();
        await c.CloseAsync();
    }
}