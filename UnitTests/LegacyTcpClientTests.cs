namespace UnitTests;

using Net;
using Net.Connection.Channels;
using Net.Connection.Clients.LegacyTcp;
using Net.Connection.Servers;
using Net.Messages;
using System.Net;
using System.Text;

public class LegacyTcpClients
{    
    private LegacyServer NewServer(bool encrypted)
    {
        var settings = encrypted ? new ServerSettings() : new ServerSettings { UseEncryption = false };

        return new LegacyServer(new IPEndPoint(IPAddress.Loopback, 0), settings);
    }

    [Fact]
    public void Connect()
    {
        var server = NewServer(true);
        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        var connected = c.Connect();
        Assert.True(connected);

        server.ShutDown();
    }

    [Fact]
    public async Task ConnectAsync()
    {
        var server = NewServer(true);
        server.Start();
        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        var connected = await c.ConnectAsync();
        Assert.True(connected);

        server.ShutDown();
    }

    [Fact]
    public async Task DisconnectsGracefully()
    {
        var server = NewServer(true);

        Action<LegacyServerClient, DisconnectionInfo> del = async (LegacyServerClient client, DisconnectionInfo info) =>
        {
            Assert.Equal(DisconnectionReason.Closed, info.Reason);
            Assert.Null(info.Exception);
        };

        server.OnClientDisconnected += del;
        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendPrimitive(bool encrypt)
    {
        var server = NewServer(encrypt);

        var str = "hello world";

        Action<object, LegacyServerClient> rec = async (obj, client) =>
        {
            Assert.Equal(str, obj as string);
        };

        server.OnClientObjectReceived += rec;

        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.SendObjectAsync(str);
        await Task.Delay(500);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendComplex(bool encrypt)
    {
        var server = NewServer(encrypt);

        var settings = new ServerSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false };

        Action<object, LegacyServerClient> rec = async (obj, client) =>
        {
            Assert.True(Helpers.AreEqual(settings, obj));
        };
        server.OnClientObjectReceived += rec;

        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.SendObjectAsync(settings);
        await Task.Delay(500);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendMultiDimensional(bool encrypt)
    {
        var server = NewServer(encrypt);

        var arr = new int[,]
        {
            {9, 8, 7},
            {6, 5, 4},
            {3, 2, 1}
        };

        Action<object, LegacyServerClient> rec = async (obj, client) =>
        {
            Assert.True(Helpers.AreEqual(arr, obj as int[,]));
        };
        server.OnClientObjectReceived += rec;

        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.SendObjectAsync(arr);
        await Task.Delay(500);

        server.OnClientObjectReceived -= rec;

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendJagged(bool encrypt)
    {
        var server = NewServer(encrypt);

        var arr = new int[][]
        {
            new int[] {9, 8, 7},
            new int[] {6, 5, 4, 69, 6, 555},
            new int[] {3, 2, 1}
        };

        Action<object, LegacyServerClient> rec = async (obj, client) =>
        {
            Assert.True(Helpers.AreEqual(arr, obj as int[][]));
        };
        server.OnClientObjectReceived += rec;

        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.SendObjectAsync(arr);
        await Task.Delay(500);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerSendComplex(bool encrypt)
    {
        var server = NewServer(encrypt);
        var settings = new ServerSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false };

        Action<LegacyServerClient> conn = async (sc) =>
        {
            await sc.SendObjectAsync(settings);
        };
        server.OnClientConnected += conn;

        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        c.OnReceiveObject += async (obj) =>
        {
            Assert.True(Helpers.AreEqual(settings,obj));
        };

        await c.ConnectAsync();
        await Task.Delay(500);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerSendJagged(bool encrypt)
    {
        var server = NewServer(encrypt);

        var arr = new int[][]
        {
            new int[] {9, 8, 7},
            new int[] {6, 5, 4, 69, 6, 555},
            new int[] {3, 2, 1}
        };

        Action<LegacyServerClient> conn = async (sc) =>
        {
            await sc.SendObjectAsync(arr);
        };
        server.OnClientConnected += conn;

        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        c.OnReceiveObject += async (obj) =>
        {
            Assert.True(Helpers.AreEqual(arr, obj));
        };

        await Task.Delay(500);

        server.ShutDown();
    }

    [Theory]
    [InlineData(typeof(UdpChannel))]
    [InlineData(typeof(EncryptedTcpChannel))]
    [InlineData(typeof(TcpChannel))]
    public async Task ClientOpenChannelAsync(Type channelType)
    {
        var server = NewServer(true);

        bool s = false;

        Action<IChannel, LegacyServerClient> opened = (ch, sc) =>
        {
            s = true;
            ch.SendBytes(Encoding.UTF8.GetBytes("Hello World"));
        };
        server.OnClientChannelOpened += opened;
        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        await c.ConnectAsync();

        IChannel ch = null;
        if (channelType == typeof(UdpChannel))
            ch = await c.OpenChannelAsync<UdpChannel>();
        else if (channelType == typeof(TcpChannel))
            ch = await c.OpenChannelAsync<TcpChannel>();
        else if (channelType == typeof(EncryptedTcpChannel))
            ch = await c.OpenChannelAsync<EncryptedTcpChannel>();

        await Task.Delay(500);

        var text = Encoding.UTF8.GetString(await ch.ReceiveBytesAsync());
        Assert.True(s);
        Assert.NotNull(ch);
        Assert.Single(c.Channels);
        Assert.Single(server.Clients[^1].Channels);
        Assert.Equal("Hello World", text);

        server.ShutDown();
    }

    [Theory]
    [InlineData(typeof(UdpChannel))]
    [InlineData(typeof(TcpChannel))]
    public async Task ClientCloseChannelAsync(Type channelType)
    {
        var server = NewServer(true);

        bool s = false;

        Action<IChannel, LegacyServerClient> opened = (ch, sc) =>
        {
            s = true;
        };

        server.OnClientChannelOpened += opened;
        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        await c.ConnectAsync();

        IChannel ch = null;
        if (channelType == typeof(UdpChannel))
            ch = await c.OpenChannelAsync<UdpChannel>();
        else if (channelType == typeof(TcpChannel))
            ch = await c.OpenChannelAsync<TcpChannel>();

        c.CloseChannel(ch);

        await Task.Delay(10);
        Assert.True(c.Channels.Count == 0 && server.Clients[0].Channels.Count == 0);

        server.ShutDown();
    }

    [Theory]
    [InlineData(typeof(UdpChannel))]
    [InlineData(typeof(TcpChannel))]
    [InlineData(typeof(EncryptedTcpChannel))]
    public async Task ChannelSendsData(Type cType)
    {
        var server = NewServer(true);

        bool s = false;

        TaskCompletionSource tcs = new TaskCompletionSource();

        Action<IChannel, LegacyServerClient> opened = async (ch, sc) =>
        {
            if (Encoding.UTF8.GetString(await ch.ReceiveBytesAsync()) == "Hello World")
                s = true;
        };
        server.OnClientChannelOpened += opened;
        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        await c.ConnectAsync();
        IChannel ch = cType switch
        {
            _ when cType == typeof(UdpChannel) => await c.OpenChannelAsync<UdpChannel>(),
            _ when cType == typeof(TcpChannel) => 
                await c.OpenChannelAsync<TcpChannel>(),
            _ when cType == typeof(EncryptedTcpChannel) => await c.OpenChannelAsync<EncryptedTcpChannel>(),
        };
        await ch.SendBytesAsync(Encoding.UTF8.GetBytes("Hello World"));
        await Task.Delay(500);
        Assert.True(s && ch is not null);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ClientSendsCustomMessage(bool encrypt)
    {
        var server = NewServer(encrypt);
        var msg = new TestMessage
        {
            Guid = Guid.NewGuid(),
            Name = "name"
        };

        Action<MessageBase, LegacyServerClient> msgB = async (m, sc) =>
        {
            Assert.True(Helpers.AreEqual(msg, m));
        };
        server.OnUnregisteredMessage += msgB;

        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        await c.ConnectAsync();
        c.SendMessage(msg);
        await Task.Delay(500);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerSendsCustomMessage(bool encrypt)
    {
        var server = NewServer(encrypt);
        var msg = new TestMessage
        {
            Guid = Guid.NewGuid(),
            Name = "name"
        };

        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        c.OnUnregisteredMessage += async (m) =>
        {
            Assert.True(Helpers.AreEqual(msg, m));
        };

        await c.ConnectAsync();
        server.SendMessageToAll(msg);
        await Task.Delay(500);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ClientOpensCustomChannel(bool encrypt)
    {
        var opened = 0;
        var managed = 0;
        var closed = 0;

        LegacyObjectClient cl = null;
        LegacyServerClient sc = null;
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

        var server = NewServer(encrypt);

        Action<LegacyServerClient> con = (c) =>
        {
            c.RegisterChannelType<DummyChannel>(open, management, close);
            sc = c;
        };
        server.OnClientConnected += con;
        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        c.RegisterChannelType<DummyChannel>(open, management, close);

        cl = c;
        await c.ConnectAsync();

        await Task.Delay(150);

        var d = await cl.OpenChannelAsync<DummyChannel>();
        cl.CloseChannel(d);
        cl = sc;
        d = await cl.OpenChannelAsync<DummyChannel>();
        cl.CloseChannel(d);
        await Task.Delay(750);

        Assert.True(opened == 2 && closed == 2 && managed == 2);
        Assert.Equal(2, opened);
        Assert.Equal(2, managed);
        Assert.Equal(2, closed);

        server.ShutDown();
    }

    [Fact]
    async Task GenericObjectEventTriggered()
    {
        var server = NewServer(true);

        var settings = new ServerSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false };

        Action<LegacyServerClient> conn = async (sc) =>
        {
            await sc.SendObjectAsync(settings);
        };
        server.OnClientConnected += conn;

        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        c.RegisterReceive<ServerSettings>(obj =>
        {
            Assert.True(Helpers.AreEqual(settings, obj));
        });

        await c.ConnectAsync();
        await Task.Delay(500);

        server.ShutDown();
    }

    [Fact]
    async Task GenericObjectEventTriggeredAsync()
    {
        var server = NewServer(true);

        var settings = new ServerSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false };

        Action<LegacyServerClient> conn = async (sc) =>
        {
            await sc.SendObjectAsync(settings);
        };
        server.OnClientConnected += conn;

        server.Start();

        var c = new LegacyClient(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        c.RegisterReceiveAsync<ServerSettings>(obj =>
        {
            Assert.True(Helpers.AreEqual(settings, obj));
            return Task.CompletedTask;
        });

        await c.ConnectAsync();
        await Task.Delay(500);

        server.ShutDown();
    }
}