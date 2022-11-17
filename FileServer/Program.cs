using FileServer;
using Net;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using Net.Messages;
using System.Net;

// NOTE: This doesn't work for large files. For that, you would have to send the file in multiple segments and reassemble it on the client

var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());
var endpoints = new List<IPEndPoint>();

foreach (var address in addresses)
    endpoints.Add(new IPEndPoint(address, 6969));

endpoints.AddRange(new[] { new IPEndPoint(IPAddress.Any, 6969), new IPEndPoint(IPAddress.IPv6Any, 6969) });

var server = new Server(endpoints, 5, new NetSettings { UseEncryption = true, ConnectionPollTimeout = 50000 });

var workingDirectory = @$"{Directory.GetCurrentDirectory()}\Files";
if (!Directory.Exists(workingDirectory)) 
    Directory.CreateDirectory(workingDirectory);

server.OnClientConnected += OnConnect;

server.OnClientDisconnected += OnDisconnect;

server.CustomMessageHandlers.Add(nameof(FileRequestMessage), HandleFileRequest);

await server.StartAsync();

foreach (var endpoint in endpoints)
    Console.WriteLine($"Hosting on {endpoint}");

Console.ReadLine();
await server.ShutDownAsync();

Tree GetTree(string dir)
{
    var tree = new Tree()
    {
        Nodes = new List<Tree>() 
    };

    foreach (var file in Directory.EnumerateFiles(dir))
        tree.Nodes.Add(new Tree() { Value = file.Split('\\')[^1] });

    foreach (var folder in Directory.EnumerateDirectories(dir))
    {
        var tr = GetTree(folder);
        tr.Value = folder.Split('\\')[^1];
        tree.Nodes.Add(tr);
    }
    return tree;
}

void OnConnect(ServerClient sc)
{
    Console.WriteLine($"{sc.LocalEndpoint} connected");
}

void OnDisconnect (ServerClient sc, bool g)
{
    Console.WriteLine($"{sc.LocalEndpoint} disconnected {(g ? "gracefully" : "ungracefully")}");
}

async void HandleFileRequest (MessageBase msg, ServerClient c)
{
    var fMsg = msg as FileRequestMessage;

    switch (fMsg.RequestType)
    {
        case FileRequestMessage.FileRequestType.Download:
            using (FileStream fs = File.OpenRead(@$"{workingDirectory}\{fMsg.PathRequest}"))
            {
                var newMsg = new FileRequestMessage() { RequestType = FileRequestMessage.FileRequestType.Upload, FileName = fMsg.PathRequest.Split('\\')[^1] };
                newMsg.FileData = new byte[fs.Length];
                await fs.ReadAsync(newMsg.FileData);
                await c.SendMessageAsync(newMsg);
                //var buffer = new byte[fs.Length];
                //await fs.ReadAsync(buffer);
                //var g = await c.OpenChannelAsync();
                //await c.SendBytesOnChannelAsync(buffer, g);
                Console.WriteLine($"{c.RemoteEndpoint} requested {fMsg.PathRequest.Split('\\')[^1]}");
            }
            break;
        case FileRequestMessage.FileRequestType.Upload:
            Directory.CreateDirectory(@$"{workingDirectory}\{fMsg.PathRequest}");
            using (FileStream fs = File.Create(@$"{workingDirectory}\{fMsg.PathRequest}\{fMsg.FileName}"))
            {
                await fs.WriteAsync(fMsg.FileData);
                var tree = GetTree(workingDirectory);
                tree.Value = "Root";
                await c.SendObjectAsync(tree);
                Console.WriteLine($"{c.RemoteEndpoint} uploaded {fMsg.FileName}");
            }
            break;
        case FileRequestMessage.FileRequestType.Tree:
            {
                var tree = GetTree(workingDirectory);
                tree.Value = "Root";
                await c.SendObjectAsync(tree);
            }
            break;
    }
}