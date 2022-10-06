namespace UnitTests;

using Net;
using Net.Connection.Channels;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using Net.Messages;
using System.Net;
using System.Text;

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendPrimitive(bool encrypt)
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1, new NetSettings { UseEncryption = encrypt });
        var str = "hello world";

        server.OnClientObjectReceived += async (obj, client) =>
        {
            await server.ShutDownAsync();
            Assert.Equal(str, obj as string);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.SendObjectAsync(str);
        await Task.Delay(500);
        await server.ShutDownAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendComplex(bool encrypt)
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1, new NetSettings { UseEncryption = encrypt });
        var settings = new NetSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false };

        server.OnClientObjectReceived += async (obj, client) =>
        {
            await server.ShutDownAsync();
            Assert.True(Helpers.AreEqual(settings, obj));
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.SendObjectAsync(settings);
        await Task.Delay(500);
        await server.ShutDownAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendMultiDimensional(bool encrypt)
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1, new NetSettings { UseEncryption = encrypt });
        var arr = new int[,]
        {
            {9, 8, 7},
            {6, 5, 4},
            {3, 2, 1}
        };

        server.OnClientObjectReceived += async (obj, client) =>
        {
            await server.ShutDownAsync();
            Assert.True(Helpers.AreEqual(arr, obj as int[,]));
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.SendObjectAsync(arr);
        await Task.Delay(500);
        await server.ShutDownAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendJagged(bool encrypt)
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1, new NetSettings { UseEncryption = encrypt });
        var arr = new int[][]
        {
            new int[] {9, 8, 7},
            new int[] {6, 5, 4, 69, 6, 555},
            new int[] {3, 2, 1}
        };

        server.OnClientObjectReceived += async (obj, client) =>
        {
            await server.ShutDownAsync();
            Assert.True(Helpers.AreEqual(arr, obj as int[][]));
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        await c.SendObjectAsync(arr);
        await Task.Delay(500);
        await server.ShutDownAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerSendComplex(bool encrypt)
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1, new NetSettings { UseEncryption = encrypt });
        var settings = new NetSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false };

        server.OnClientConnected += async (sc) =>
        {
            await sc.SendObjectAsync(settings);
        };
        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        c.OnReceiveObject += async (obj) =>
        {
            Assert.True(Helpers.AreEqual(settings,obj));
        };

        await c.ConnectAsync();
        await Task.Delay(500);

        await server.ShutDownAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerSendJagged(bool encrypt)
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1, new NetSettings { UseEncryption = encrypt });
        var arr = new int[][]
        {
            new int[] {9, 8, 7},
            new int[] {6, 5, 4, 69, 6, 555},
            new int[] {3, 2, 1}
        };

        server.OnClientConnected += async (sc) =>
        {
            await sc.SendObjectAsync(arr);
        };

        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        await c.ConnectAsync();
        c.OnReceiveObject += async (obj) =>
        {
            Assert.True(Helpers.AreEqual(arr, obj));
        };

        await Task.Delay(500);
        await server.ShutDownAsync();
    }

    [Theory]
    [InlineData(typeof(UdpChannel))]
    [InlineData(typeof(TcpChannel))]
    public async Task ClientOpenChannelAsync(Type channelType)
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1);
        server.Start();
        bool s = false;

        server.OnClientChannelOpened += (ch, sc) =>
        {
            s = true;
            ch.SendBytes(Encoding.UTF8.GetBytes("Hello World"));
        };

        var c = new Client(IPAddress.Loopback, port++);

        await c.ConnectAsync();

        IChannel ch = null;
        if (channelType == typeof(UdpChannel))
            ch = await c.OpenChannelAsync<UdpChannel>();
        else if (channelType == typeof(TcpChannel))
            ch = await c.OpenChannelAsync<TcpChannel>();

        var text = Encoding.UTF8.GetString(await ch.ReceiveBytesAsync());
        Assert.True(s && ch is not null && c.Channels.Count == 1 && server.Clients[0].Channels.Count == 1 && text == "Hello World");
        await server.ShutDownAsync();
    }

    [Theory]
    [InlineData(typeof(UdpChannel))]
    [InlineData(typeof(TcpChannel))]
    public async Task ClientCloseChannelAsync(Type channelType)
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

        IChannel ch = null;
        if (channelType == typeof(UdpChannel))
            ch = await c.OpenChannelAsync<UdpChannel>();
        else if (channelType == typeof(TcpChannel))
            ch = await c.OpenChannelAsync<TcpChannel>();

        c.CloseChannel(ch);

        await Task.Delay(10);
        Assert.True(c.Channels.Count == 0 && server.Clients[0].Channels.Count == 0);
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
        await Task.Delay(500);
        Assert.True(s && ch is not null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ClientSendsCustomMessage(bool encrypt)
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1, new NetSettings { UseEncryption = encrypt });
        var msg = new TestMessage
        {
            Guid = Guid.NewGuid(),
            Name = "name"
        };

        server.OnUnregisteredMessage += async (m, sc) =>
        {
            Assert.True(Helpers.AreEqual(msg, m));
        };

        server.Start();

        var c = new Client(IPAddress.Loopback, port++);

        await c.ConnectAsync();
        await Task.Delay(500);

        await server.ShutDownAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerSendsCustomMessage(bool encrypt)
    {
        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1, new NetSettings { UseEncryption = encrypt });
        var msg = new TestMessage
        {
            Guid = Guid.NewGuid(),
            Name = "name"
        };

        server.Start();

        var c = new Client(IPAddress.Loopback, port++);

        c.OnUnregisteredMessage += async (m) =>
        {
            Assert.True(Helpers.AreEqual(msg, m));
        };

        await c.ConnectAsync();
        await Task.Delay(500);
        
        await server.ShutDownAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ClientOpensCustomChannel(bool encrypt)
    {
        var opened = 0;
        var managed = 0;
        var closed = 0;

        ObjectClient cl = null;
        ServerClient sc = null;
        Func<Task<DummyChannel>> open = () =>
        {
            opened++;
            cl.SendMessage(new ChannelManagementMessage() { Type = typeof(DummyChannel).Name });
            return Task.FromResult(new DummyChannel());
        };
        Func<ChannelManagementMessage, Task> management = (cm) =>
        {
            managed++;
            return Task.CompletedTask;
        };
        Func<DummyChannel, Task> close = (dc) =>
        {
            closed++;
            return Task.CompletedTask;
        };

        var server = new Server(new IPEndPoint(IPAddress.Loopback, port), 1, new NetSettings { UseEncryption = encrypt });
        server.OnClientConnected += (c) =>
        {
            c.RegisterChannelType<DummyChannel>(open, management, close);
            sc = c;
        };

        server.Start();

        var c = new Client(IPAddress.Loopback, port++);
        c.RegisterChannelType<DummyChannel>(open, management, close);

        cl = c;
        await c.ConnectAsync();
        var d = await cl.OpenChannelAsync<DummyChannel>();
        cl.CloseChannel(d);
        cl = sc;
        d = await cl.OpenChannelAsync<DummyChannel>();
        cl.CloseChannel(d);
        await Task.Delay(500);

        Assert.True(opened == 2 && closed == 2 && managed == 2);
        await server.ShutDownAsync();
    }
}