using Net.Connection.Clients;
using Net.Connection.Servers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

var endpoints = new List<IPEndPoint>
{
    new IPEndPoint(IPAddress.Any, 5555),
    new IPEndPoint(IPAddress.IPv6Any, 5555)
};

// Initialize server to listen on all available addresses with a maximum of 5 clients
Server s = new Server(endpoints, 5, new Net.NetSettings { UseEncryption = true, ConnectionPollTimeout = 10000, SingleThreadedServer = false});

s.OnClientConnected = Connected;
s.OnClientDisconnected = Disconnected;
s.OnClientObjectReceived += Recieved;

// Starts listening for incomming connections
s.Start();

Console.WriteLine($"Hosting on {s.Endpoints[0].Address}");
Console.WriteLine();
foreach (var addr in Dns.GetHostAddresses(Dns.GetHostName()))
{
    Console.WriteLine($"Valid address: {addr}");
}
Console.WriteLine();

while (true)
{
    var l = Console.ReadLine();
    if (l.ToLowerInvariant().StartsWith("send channel"))
        await (await s.Clients[0].OpenChannelAsync()).SendBytesAsync(Encoding.UTF8.GetBytes(l.Substring(13)));
    else if (l == "EXIT")
    {
        s.ShutDown();
        break;
    }
    else
        s.SendObjectToAll(l);
}

await Task.Delay(1000);

void Disconnected(ServerClient sc, bool graceful) =>
    Console.WriteLine($"Disconnected {(graceful ? "gracefully" : "ungracefully")}: {sc.RemoteEndpoint}");

void Connected(ServerClient c) =>
    Console.WriteLine($"Connected: {c.RemoteEndpoint}");

async void Recieved(object obj, ServerClient c)
{
    foreach (ObjectClient b in s.Clients)
        if (b != c)
            await b.SendObjectAsync(obj);
}