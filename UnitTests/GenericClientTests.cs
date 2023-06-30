namespace UnitTests;

using Net.Connection.Channels;
using Net.Connection.Clients.Generic;
using Net.Connection.Servers.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;


public class GenericClientTests
{
    [Fact]
    public async void Connect()
    {
        int port = 0;
        var server = new Server();
        bool connected = false;
        server.RegisterConnectionMethod<TcpChannel>(async () =>
        {
            Socket servSoc = new Socket(SocketType.Stream, ProtocolType.Tcp);
            servSoc.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            servSoc.Listen();
            port = ((IPEndPoint)servSoc.LocalEndPoint).Port;

            var soc = await servSoc.AcceptAsync();
            var chan = new TcpChannel(soc);
            var client = new ServerClient(chan);
            return client;
        });
        server.OnClientConnected += (client) =>
        {
            connected = true;
        };
        server.Start();

        var c = new Client();
        c.ConnectMethod = () =>
        {
            var soc = new Socket(SocketType.Stream, ProtocolType.Tcp);
            soc.Connect(new IPEndPoint(IPAddress.Loopback, port));
            c.Connection = new TcpChannel(soc);
            return true;
        };
        var conn = c.Connect();
        await Task.Delay(500);
        Assert.True(connected && conn);
        server.ShutDown();
    }

    [Fact]
    public async Task ConnectAsync()
    {
        int port = 0;
        var server = new Server();
        bool connected = false;
        server.RegisterConnectionMethod<TcpChannel>(async () =>
        {
            Socket servSoc = new Socket(SocketType.Stream, ProtocolType.Tcp);
            servSoc.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            servSoc.Listen();
            port = ((IPEndPoint)servSoc.LocalEndPoint).Port;

            var soc = await servSoc.AcceptAsync();
            var chan = new TcpChannel(soc);
            var client = new ServerClient(chan);
            return client;
        });
        server.OnClientConnected += (client) =>
        {
            connected = true;
        };
        server.Start();

        var c = new Client();
        c.ConnectMethodAsync = async () =>
        {
            var soc = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await soc.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port));
            c.Connection = new TcpChannel(soc);
            return true;
        };
        var conn = await c.ConnectAsync();
        await Task.Delay(1000);
        Assert.True(connected && conn);
    }
}