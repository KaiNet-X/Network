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
    private static Server decrypted
    {
        get
        {
            _decrypted ??= new Server(new IPEndPoint(IPAddress.Loopback, 0), 20, new ServerSettings
            {
                UseEncryption = false,
            });
            if (!_decrypted.Active)
                _decrypted.Start();
            return _decrypted;
        }
    }
    private static Server _decrypted;

    private static Server encrypted
    {
        get
        {
            _encrypted ??= new Server(new IPEndPoint(IPAddress.Loopback, 0), 20, new ServerSettings
            {
                UseEncryption = true,
            });
            if (!_encrypted.Active)
                _encrypted.Start();
            return _encrypted;
        }
    }
    private static Server _encrypted;
    
    [Fact]
    public void Connect()
    {
        var c = new Client(IPAddress.Loopback, encrypted.ActiveEndpoints[0].Port);
        var connected = c.Connect();
        c.Close();
        Assert.True(connected);
    }

    [Fact]
    public async Task ConnectAsync()
    {
        var c = new Client(IPAddress.Loopback, encrypted.ActiveEndpoints[0].Port);
        var connected = await c.ConnectAsync();
        await c.CloseAsync();
        Assert.True(connected);
    }

    [Fact]
    public async Task DisconnectsGracefully()
    {
        Action<ServerClient, bool> del = async (ServerClient client, bool graceful) =>
        {
            Assert.True(graceful);
        };

        encrypted.OnClientDisconnected += del;

        var c = new Client(IPAddress.Loopback, encrypted.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.CloseAsync();
        encrypted.OnClientDisconnected -= del;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendPrimitive(bool encrypt)
    {
        var server = encrypt ? encrypted : decrypted;
        var str = "hello world";

        Action<object, ServerClient> rec = async (obj, client) =>
        {
            Assert.Equal(str, obj as string);
        };

        server.OnClientObjectReceived += rec;

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.SendObjectAsync(str);
        await Task.Delay(500);
        await c.CloseAsync();

        server.OnClientObjectReceived -= rec;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendComplex(bool encrypt)
    {
        var server = encrypt ? encrypted : decrypted;
        var settings = new ServerSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false };

        Action<object, ServerClient> rec = async (obj, client) =>
        {
            Assert.True(Helpers.AreEqual(settings, obj));
        };
        server.OnClientObjectReceived += rec;

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.SendObjectAsync(settings);
        await Task.Delay(500);
        await c.CloseAsync();

        server.OnClientObjectReceived -= rec;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendMultiDimensional(bool encrypt)
    {
        var server = encrypt ? encrypted : decrypted;
        var arr = new int[,]
        {
            {9, 8, 7},
            {6, 5, 4},
            {3, 2, 1}
        };

        Action<object, ServerClient> rec = async (obj, client) =>
        {
            Assert.True(Helpers.AreEqual(arr, obj as int[,]));
        };
        server.OnClientObjectReceived += rec;

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.SendObjectAsync(arr);
        await Task.Delay(500);
        await c.CloseAsync();

        server.OnClientObjectReceived -= rec;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendJagged(bool encrypt)
    {
        var server = encrypt ? encrypted : decrypted;
        var arr = new int[][]
        {
            new int[] {9, 8, 7},
            new int[] {6, 5, 4, 69, 6, 555},
            new int[] {3, 2, 1}
        };

        Action<object, ServerClient> rec = async (obj, client) =>
        {
            Assert.True(Helpers.AreEqual(arr, obj as int[][]));
        };
        server.OnClientObjectReceived += rec;

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.SendObjectAsync(arr);
        await Task.Delay(500);
        await c.CloseAsync();

        server.OnClientObjectReceived -= rec;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerSendComplex(bool encrypt)
    {
        var server = encrypt ? encrypted : decrypted;
        var settings = new ServerSettings() { ConnectionPollTimeout = int.MaxValue, UseEncryption = false };

        Action<ServerClient> conn = async (sc) =>
        {
            await sc.SendObjectAsync(settings);
        };
        server.OnClientConnected += conn;

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        c.OnReceiveObject += async (obj) =>
        {
            Assert.True(Helpers.AreEqual(settings,obj));
        };

        await c.ConnectAsync();
        await Task.Delay(500);
        await c.CloseAsync();

        server.OnClientConnected -= conn;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerSendJagged(bool encrypt)
    {
        var server = encrypt ? encrypted : decrypted;
        var arr = new int[][]
        {
            new int[] {9, 8, 7},
            new int[] {6, 5, 4, 69, 6, 555},
            new int[] {3, 2, 1}
        };

        Action<ServerClient> conn = async (sc) =>
        {
            await sc.SendObjectAsync(arr);
        };
        server.OnClientConnected += conn;

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        c.OnReceiveObject += async (obj) =>
        {
            Assert.True(Helpers.AreEqual(arr, obj));
        };

        await Task.Delay(500);
        await c.CloseAsync();

        server.OnClientConnected -= conn;
    }

    [Theory]
    [InlineData(typeof(UdpChannel))]
    [InlineData(typeof(TcpChannel))]
    public async Task ClientOpenChannelAsync(Type channelType)
    {
        bool s = false;

        Action<IChannel, ServerClient> opened = (ch, sc) =>
        {
            s = true;
            ch.SendBytes(Encoding.UTF8.GetBytes("Hello World"));
        };
        encrypted.OnClientChannelOpened += opened;

        var c = new Client(IPAddress.Loopback, encrypted.ActiveEndpoints[0].Port);

        await c.ConnectAsync();

        IChannel ch = null;
        if (channelType == typeof(UdpChannel))
            ch = await c.OpenChannelAsync<UdpChannel>();
        else if (channelType == typeof(TcpChannel))
            ch = await c.OpenChannelAsync<TcpChannel>();

        await Task.Delay(250);

        var text = Encoding.UTF8.GetString(await ch.ReceiveBytesAsync());
        Assert.True(s);
        Assert.NotNull(ch);
        Assert.Single(c.Channels);
        Assert.Single(encrypted.Clients[^1].Channels);
        Assert.Equal("Hello World", text);
        await c.CloseAsync();

        encrypted.OnClientChannelOpened -= opened;
    }

    [Theory]
    [InlineData(typeof(UdpChannel))]
    [InlineData(typeof(TcpChannel))]
    public async Task ClientCloseChannelAsync(Type channelType)
    {
        bool s = false;

        Action<IChannel, ServerClient> opened = (ch, sc) =>
        {
            s = true;
        };
        encrypted.OnClientChannelOpened += opened;

        var c = new Client(IPAddress.Loopback, encrypted.ActiveEndpoints[0].Port);

        await c.ConnectAsync();

        IChannel ch = null;
        if (channelType == typeof(UdpChannel))
            ch = await c.OpenChannelAsync<UdpChannel>();
        else if (channelType == typeof(TcpChannel))
            ch = await c.OpenChannelAsync<TcpChannel>();

        c.CloseChannel(ch);

        await Task.Delay(10);
        Assert.True(c.Channels.Count == 0 && encrypted.Clients[0].Channels.Count == 0);
        await c.CloseAsync();

        encrypted.OnClientChannelOpened -= opened;
    }

    [Fact]
    public async Task ChannelSendsData()
    {
        bool s = false;

        TaskCompletionSource tcs = new TaskCompletionSource();


        Action<IChannel, ServerClient> opened = async (ch, sc) =>
        {
            if (Encoding.UTF8.GetString(await ch.ReceiveBytesAsync()) == "Hello World")
                s = true;
        };
        encrypted.OnClientChannelOpened += opened;

        var c = new Client(IPAddress.Loopback, encrypted.ActiveEndpoints[0].Port);

        await c.ConnectAsync();
        var ch = await c.OpenChannelAsync<UdpChannel>();
        await ch.SendBytesAsync(Encoding.UTF8.GetBytes("Hello World"));
        await Task.Delay(500);
        Assert.True(s && ch is not null);
        await c.CloseAsync();

        encrypted.OnClientChannelOpened -= opened;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ClientSendsCustomMessage(bool encrypt)
    {
        var server = encrypt ? encrypted : decrypted;
        var msg = new TestMessage
        {
            Guid = Guid.NewGuid(),
            Name = "name"
        };

        Action<MessageBase, ServerClient> msgB = async (m, sc) =>
        {
            Assert.True(Helpers.AreEqual(msg, m));
        };
        server.OnUnregisteredMessage += msgB;

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        await c.ConnectAsync();
        c.SendMessage(msg);
        await Task.Delay(500);
        await c.CloseAsync();

        server.OnUnregisteredMessage -= msgB;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerSendsCustomMessage(bool encrypt)
    {
        var server = encrypt ? encrypted : decrypted;
        var msg = new TestMessage
        {
            Guid = Guid.NewGuid(),
            Name = "name"
        };

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        c.OnUnregisteredMessage += async (m) =>
        {
            Assert.True(Helpers.AreEqual(msg, m));
        };

        await c.ConnectAsync();
        server.SendMessageToAll(msg);
        await Task.Delay(500);
        await c.CloseAsync();
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

        var server = encrypt ? encrypted : decrypted;

        Action<ServerClient> con = (c) =>
        {
            c.RegisterChannelType<DummyChannel>(open, management, close);
            sc = c;
        };
        server.OnClientConnected += con;

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
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

        server.OnClientConnected -= con;
    }
}