namespace UnitTests;

using Net;
using Net.Connection.Channels;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using Net.Messages;
using System.Diagnostics;
using System.Net;
using System.Text;

public class TcpClientTests
{
    private TcpServer NewServer(bool encrypted)
    {
        var settings = encrypted ? 
            new ConnectionSettings 
            { 
                ServerRequiresRegisteredTypes = false 
            } : 
            new ConnectionSettings 
            {
                UseEncryption = false,
                ServerRequiresRegisteredTypes = false
            };

        return new TcpServer(new IPEndPoint(IPAddress.Loopback, 0), settings);
    }

    [Fact]
    public void Connect()
    {
        var server = NewServer(true);
        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        Assert.True(c.Connect());

        server.ShutDown();
    }

    [Fact]
    public async Task ConnectAsync()
    {
        var server = NewServer(true);
        server.Start();
        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        var connected = await c.ConnectAsync();
        Assert.True(connected);

        server.ShutDown();
    }

    [Fact]
    public async Task DisconnectsGracefully()
    {
        var server = NewServer(true);

        var del = (DisconnectionInfo info, ServerClient client) =>
        {
            Assert.Equal(DisconnectionReason.Closed, info.Reason);
            Assert.Null(info.Exception);
        };

        server.OnDisconnect(del);
        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendPrimitive(bool encrypt)
    {
        var tcs = new TaskCompletionSource<bool>();

        var server = NewServer(encrypt);

        var str = "hello world";

        server.RegisterReceive<string>((obj, client) =>
        {
            tcs.SetResult(str == obj);
        });

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.SendObjectAsync(str);

        var t = await Task.WhenAny(Task.Delay(500), tcs.Task);
        Assert.True(t == tcs.Task && tcs.Task.Result);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendComplex(bool encrypt)
    {
        var tcs = new TaskCompletionSource<bool>();
        var server = NewServer(encrypt);

        var settings = new ConnectionSettings() { ConnectionPollTimeout = -1, UseEncryption = false, ServerRequiresRegisteredTypes = false };

        server.RegisterReceive<ConnectionSettings>((obj, client) =>
        {
            tcs.SetResult(Helpers.AreEqual(settings, obj));
        });

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.SendObjectAsync(settings);

        var t = await Task.WhenAny(Task.Delay(500), tcs.Task);
        Assert.True(t == tcs.Task && tcs.Task.Result);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendMultiDimensional(bool encrypt)
    {
        var tcs = new TaskCompletionSource<bool>();

        var server = NewServer(encrypt);

        var arr = new int[,]
        {
            {9, 8, 7},
            {6, 5, 4},
            {3, 2, 1}
        };

        server.RegisterReceive<int[,]>((obj, client) =>
        {
            tcs.SetResult(Helpers.AreEqual(arr, obj));
        });

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.SendObjectAsync(arr);

        var t = await Task.WhenAny(Task.Delay(500), tcs.Task);
        Assert.True(t == tcs.Task && tcs.Task.Result);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendJagged(bool encrypt)
    {
        var tcs = new TaskCompletionSource();

        var server = NewServer(encrypt);

        var arr = new int[][]
        {
            [9, 8, 7],
            [6, 5, 4, 69, 6, 555],
            [3, 2, 1]
        };

        Action<object, ServerClient> rec = (obj, client) =>
        {
            tcs.SetResult();
            Assert.True(Helpers.AreEqual(arr, obj));
        };

        server.RegisterReceive<int[][]>(rec);

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        await c.ConnectAsync();
        await c.SendObjectAsync(arr);

        await Task.WhenAny(Task.Delay(500), tcs.Task);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerSendComplex(bool encrypt)
    {
        var tcs = new TaskCompletionSource<bool>();

        var server = NewServer(encrypt);

        var settings = new ConnectionSettings() { ConnectionPollTimeout = -1, UseEncryption = false };

        Action<ServerClient> conn = async (sc) =>
        {
            await sc.SendObjectAsync(settings);
        };

        server.OnClientConnected += conn;

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        c.RegisterReceive<ConnectionSettings>(obj =>
        {
            tcs.SetResult(Helpers.AreEqual(settings, obj));
        });

        await c.ConnectAsync();

        var t = await Task.WhenAny(Task.Delay(500), tcs.Task);
        Assert.True(t == tcs.Task && tcs.Task.Result);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerSendJagged(bool encrypt)
    {
        var tcs = new TaskCompletionSource();

        var server = NewServer(encrypt);

        var arr = new int[][]
        {
            [9, 8, 7],
            [6, 5, 4, 69, 6, 555],
            [3, 2, 1]
        };

        Action<ServerClient> conn = (sc) =>
        {
            sc.SendObject(arr);
        };

        server.OnClientConnected += conn;

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        c.RegisterReceive<int[][]>((obj) =>
        {
            tcs.SetResult();
            Assert.True(Helpers.AreEqual(arr, obj));
        });

        await c.ConnectAsync();

        await Task.WhenAny(Task.Delay(500), tcs.Task);

        server.ShutDown();
    }

    [Theory]
    [InlineData(typeof(UdpChannel))]
    [InlineData(typeof(EncryptedTcpChannel))]
    [InlineData(typeof(TcpChannel))]
    public async Task ClientOpenChannelAsync(Type channelType)
    {
        var tcs = new TaskCompletionSource();

        var server = NewServer(true);

        bool s = false;

        Action<BaseChannel, ServerClient> opened = (ch, sc) =>
        {
            s = true;
            tcs.SetResult();
            ch.SendBytes(Encoding.UTF8.GetBytes("Hello World"));
        };

        server.OnAnyChannel(opened);
        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        await c.ConnectAsync();

        BaseChannel ch = channelType switch
        {
            _ when channelType == typeof(UdpChannel) => await c.OpenChannelAsync<UdpChannel>(),
            _ when channelType == typeof(TcpChannel) => await c.OpenChannelAsync<TcpChannel>(),
            _ when channelType == typeof(EncryptedTcpChannel) => await c.OpenChannelAsync<EncryptedTcpChannel>(),
        };

        var text = Encoding.UTF8.GetString(await ch.ReceiveBytesAsync());

        var t = await Task.WhenAny(Task.Delay(500), tcs.Task);
        Assert.True(t == tcs.Task);

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
    [InlineData(typeof(EncryptedTcpChannel))]
    public async Task ClientCloseChannelAsync(Type channelType)
    {
        var server = NewServer(true);

        bool s = false;

        Action<BaseChannel, ServerClient> opened = (ch, sc) =>
        {
            s = true;
        };

        server.OnAnyChannel(opened);
        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        await c.ConnectAsync();

        BaseChannel ch = channelType switch
        {
            _ when channelType == typeof(UdpChannel) => await c.OpenChannelAsync<UdpChannel>(),
            _ when channelType == typeof(TcpChannel) => await c.OpenChannelAsync<TcpChannel>(),
            _ when channelType == typeof(EncryptedTcpChannel) => await c.OpenChannelAsync<EncryptedTcpChannel>(),
        };

        c.CloseChannel(ch);

        await Task.Delay(25);
        
        Assert.True(s);
        Assert.Equal(0, c.Channels.Count);
        Assert.Equal(0, server.Clients[0].Channels.Count);

        server.ShutDown();
    }

    [Theory]
    [InlineData(typeof(UdpChannel))]
    [InlineData(typeof(TcpChannel))]
    [InlineData(typeof(EncryptedTcpChannel))]
    public async Task ChannelSendsData(Type channelType)
    {
        var tcs = new TaskCompletionSource();

        var server = NewServer(true);

        Action<BaseChannel, ServerClient> opened = async (ch, sc) =>
        {
            if (Encoding.UTF8.GetString(await ch.ReceiveBytesAsync()) == "Hello World")
                tcs.SetResult();
        };
        server.OnAnyChannel(opened);
        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        await c.ConnectAsync();
        BaseChannel ch = channelType switch
        {
            _ when channelType == typeof(UdpChannel) => await c.OpenChannelAsync<UdpChannel>(),
            _ when channelType == typeof(TcpChannel) => await c.OpenChannelAsync<TcpChannel>(),
            _ when channelType == typeof(EncryptedTcpChannel) => await c.OpenChannelAsync<EncryptedTcpChannel>(),
        };

        await ch.SendBytesAsync(Encoding.UTF8.GetBytes("Hello World"));
        await Task.WhenAny(tcs.Task, Task.Delay(500));

        Assert.True(tcs.Task.IsCompletedSuccessfully && ch is not null);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ClientSendsCustomMessage(bool encrypt)
    {
        var tcs = new TaskCompletionSource();

        var server = NewServer(encrypt);
        var msg = new TestMessage
        {
            Guid = Guid.NewGuid(),
            Name = "name"
        };

        Action<MessageBase, ServerClient> msgB = (m, sc) =>
        {
            Assert.True(Helpers.AreEqual(msg, m));
            tcs.SetResult();
        };
        server.OnUnregisteredMessage += msgB;

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        await c.ConnectAsync();
        c.SendMessage(msg);
        await Task.WhenAny(Task.Delay(500), tcs.Task);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerSendsCustomMessage(bool encrypt)
    {
        var tcs = new TaskCompletionSource();

        var server = NewServer(encrypt);
        var msg = new TestMessage
        {
            Guid = Guid.NewGuid(),
            Name = "name"
        };

        server.Start();

        var c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);

        c.OnUnregisteredMessage += (m) =>
        {
            Assert.True(Helpers.AreEqual(msg, m));
            tcs.SetResult();
        };

        await c.ConnectAsync();
        server.SendMessageToAll(msg);
        await Task.WhenAny(Task.Delay(500), tcs.Task);

        server.ShutDown();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ClientOpensCustomChannel(bool encrypt)
    {
        var createChannels = new TaskCompletionSource();
        var manageChannels = new TaskCompletionSource();
        var closeChannels = new TaskCompletionSource();

        var opened = 0;
        var managed = 0;
        var closed = 0;

        ServerClient sc = null;
        Client c = null;

        Func<Task<DummyChannel>> open = () =>
        {
            opened++;
            if (opened == 2)
                createChannels.SetResult();
            c.SendMessage(new ChannelManagementMessage() { Type = typeof(DummyChannel).Name });
            return Task.FromResult(new DummyChannel());
        };
        Func<ChannelManagementMessage, Task> management = (cm) =>
        {
            managed++;
            if (managed == 2)
                manageChannels.SetResult();
            return Task.CompletedTask;
        };
        Func<DummyChannel, Task> close = (dc) =>
        {
            closed++;
            if (closed == 2)
                closeChannels.SetResult();
            return Task.CompletedTask;
        };

        var server = NewServer(encrypt);

        Action<ServerClient> con = (c) =>
        {
            sc = c;
            c.RegisterChannelType<DummyChannel>(open, management, close);
        };

        server.OnClientConnected += con;
        server.Start();

        c = new Client(IPAddress.Loopback, server.ActiveEndpoints[0].Port);
        c.RegisterChannelType<DummyChannel>(open, management, close);

        await c.ConnectAsync();

        var d = await c.OpenChannelAsync<DummyChannel>();
        c.CloseChannel(d);
        d = await sc.OpenChannelAsync<DummyChannel>();
        sc.CloseChannel(d);

        await Task.WhenAny(Task.WhenAll(createChannels.Task, manageChannels.Task, closeChannels.Task), Task.Delay(750));

        Assert.Equal(2, opened);
        Assert.Equal(2, managed);
        Assert.Equal(2, closed);

        server.ShutDown();
    }
}