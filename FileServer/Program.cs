using FileServer;
using Net;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.Net;

// NOTE: This doesn't work for large files. For that, you would have to send the file in multiple segments and reassemble it on the client

const int PORT = 6969;

var workingDirectory = @$"{Directory.GetCurrentDirectory()}\Files";

var authService = new AuthService();
await authService.LoadUsersAsync();

if (!Directory.Exists(workingDirectory))
    Directory.CreateDirectory(workingDirectory);

var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());

var server = new TcpServer([new IPEndPoint(IPAddress.Any, PORT), new IPEndPoint(IPAddress.IPv6Any, PORT)]
, new ServerSettings 
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

AppDomain.CurrentDomain.ProcessExit += OnKill;

var fileService = new FileService(server, authService, workingDirectory);

foreach (var address in addresses)
    Console.WriteLine($"Hosting on {address}:{PORT}");

bool exiting = false;
do
{
    switch (Console.ReadLine()?.ToUpper())
    {
        case "EXIT":
            exiting = true;
            break;
        default:
            Console.WriteLine("Unknown command");
            break;
    }
} while (!exiting);

await server.ShutDownAsync();

void OnConnect(ServerClient sc)
{
    Console.WriteLine($"{sc.LocalEndpoint} connected");
}

void OnDisconnect (DisconnectionInfo info, ServerClient sc)
{
    Console.WriteLine($"{sc.LocalEndpoint} {info.Reason}");
}

void OnKill(object? sender, EventArgs e)
{
    server.ShutDown();
}