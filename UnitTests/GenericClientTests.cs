namespace UnitTests;

using Net.Connection.Channels;
using Net.Connection.Clients.Generic;
using Net.Connection.Servers.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


public class GenericClientTests
{
    [Fact]
    public async void Connect()
    {
        var server = new Server();
        bool connected = false;
        server.RegisterConnectionMethod<TcpChannel>(async () =>
        {
            Socket servSoc = new Socket(SocketType.Stream, ProtocolType.Tcp);
            servSoc.Bind(new IPEndPoint(IPAddress.Loopback, Helpers.WaitForPort()));
            servSoc.Listen();

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
            soc.Connect(new IPEndPoint(IPAddress.Loopback, Helpers.Port++));
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
        var server = new Server();
        bool connected = false;
        server.RegisterConnectionMethod<TcpChannel>(async () =>
        {
            Socket servSoc = new Socket(SocketType.Stream, ProtocolType.Tcp);
            servSoc.Bind(new IPEndPoint(IPAddress.Loopback, Helpers.WaitForPort()));
            servSoc.Listen();

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
            await soc.ConnectAsync(new IPEndPoint(IPAddress.Loopback, Helpers.Port++));
            c.Connection = new TcpChannel(soc);
            return true;
        };
        var conn = await c.ConnectAsync();
        await Task.Delay(500);
        Assert.True(connected && conn);
        server.ShutDown();
    }
}