using Net;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

var endpoints = new List<IPEndPoint>
{
    new IPEndPoint(IPAddress.Any, 5555),
    new IPEndPoint(IPAddress.IPv6Any, 5555),
    new IPEndPoint(IPAddress.Loopback, 5555),
    new IPEndPoint(IPAddress.IPv6Loopback, 5555)
};

// Initialize server to listen on all available addresses with a maximum of 5 clients
LegacyServer s = new LegacyServer(endpoints, new ServerSettings { UseEncryption = true, ConnectionPollTimeout = 40000, MaxClientConnections = 5 });

s.OnClientConnected += Connected;
s.OnClientDisconnected += Disconnected;
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

//while (true)
//{
//    var l = Console.ReadLine();
//    if (l.ToLowerInvariant().StartsWith("send channel"))
//        await (await s.Clients[0].OpenChannelAsync()).SendBytesAsync(Encoding.UTF8.GetBytes(l.Substring(13)));
//    else if (l == "EXIT")
//    {
//        s.ShutDown();
//        break;
//    }
//    else
//        s.SendObjectToAll(l);
//}

await Task.Delay(10000000);

void Disconnected(LegacyServerClient sc, DisconnectionInfo info) =>
    Console.WriteLine($"{info.Reason}");

void Connected(LegacyServerClient c) =>
    Console.WriteLine($"Connected: {c.RemoteEndpoint}");

async void Recieved(object obj, LegacyServerClient c)
{
    foreach (LegacyObjectClient b in s.Clients)
        if (b != c)
            await b.SendObjectAsync(obj);
}