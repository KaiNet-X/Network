using FileServer;
using Net;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.Net;

// NOTE: This doesn't work for large files. For that, you would have to send the file in multiple segments and reassemble it on the client

var workingDirectory = @$"{Directory.GetCurrentDirectory()}\Files";

var authService = new AuthService();
await authService.LoadUsersAsync();
await authService.AddUser("Kai", "Kai");
await authService.AddUser("Kai1", "Kai1");
await authService.SaveUsersAsync();

if (!Directory.Exists(workingDirectory))
    Directory.CreateDirectory(workingDirectory);

var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());
var endpoints = new List<IPEndPoint>();

foreach (var address in addresses)
    endpoints.Add(new IPEndPoint(address, 6969));

endpoints.AddRange(new[] { new IPEndPoint(IPAddress.Any, 6969), new IPEndPoint(IPAddress.IPv6Any, 6969) });

var server = new TcpServer(endpoints, new ServerSettings 
{
    UseEncryption = true, 
    ConnectionPollTimeout = 600000,
    MaxClientConnections = 5,
    ClientRequiresWhitelistedTypes = true
});

server.OnClientConnected(OnConnect);

server.OnDisconnect(OnDisconnect);

server.OnObjectError((eFrame, sc) =>
{
    Console.WriteLine($"Potential malicious payload \"{eFrame.TypeName}\" by {sc.RemoteEndpoint}");
});

server.Start();

var fileService = new FileService(server, authService, workingDirectory);

foreach (var endpoint in endpoints)
    Console.WriteLine($"Hosting on {endpoint}");

Console.ReadLine();
await server.ShutDownAsync();

void OnConnect(ServerClient sc)
{
    Console.WriteLine($"{sc.LocalEndpoint} connected");
}

void OnDisconnect (DisconnectionInfo info, ServerClient sc)
{
    Console.WriteLine($"{sc.LocalEndpoint} {info.Reason}");
}