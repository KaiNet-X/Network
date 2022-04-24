using FileServer;
using Net.Connection.Clients;
using Net.Connection.Servers;
using System.Net;

var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());
var endpoints = new List<IPEndPoint>();

foreach (var address in addresses)
    endpoints.Add(new IPEndPoint(address, 6969));

var server = new Server(endpoints, 5);

var workingDirectory = @$"{Directory.GetCurrentDirectory()}\Files";
if (!Directory.Exists(workingDirectory)) 
    Directory.CreateDirectory(workingDirectory);

server.OnClientConnected += delegate (ServerClient sc) 
{
    Console.WriteLine($"{sc.LocalEndpoint} connected");
};

server.OnClientDisconnected += delegate (ServerClient sc)
{
    Console.WriteLine($"{sc.LocalEndpoint} disconnected");
};

server.CustomMessageHandlers.Add(FileRequestMessage.Type, async (msg, c) => 
{
    var fMsg = msg as FileRequestMessage;

    switch (fMsg.RequestType)
    {
        case FileRequestMessage.FileRequestType.Download:
            using (FileStream fs = File.OpenRead(@$"{workingDirectory}\{fMsg.PathRequest}"))
            {
                var newMsg = new FileRequestMessage() { RequestType = FileRequestMessage.FileRequestType.Upload};
                await fs.ReadAsync(newMsg.FileData);
                c.SendMessage(newMsg);
            }
            break;
        case FileRequestMessage.FileRequestType.Upload:
            using (FileStream fs = File.Create(@$"{workingDirectory}\{fMsg.PathRequest}"))
            {
                await fs.WriteAsync(fMsg.FileData);
            }
            break;
    }
});

await server.StartServerAsync();

foreach (var endpoint in endpoints)
    Console.WriteLine($"Hosting on {endpoint}");

Console.ReadLine();
await server.ShutDownAsync();