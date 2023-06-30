using FileServer;
using Net;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.Net;

// NOTE: This doesn't work for large files. For that, you would have to send the file in multiple segments and reassemble it on the client

var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());
var endpoints = new List<IPEndPoint>();

foreach (var address in addresses)
    endpoints.Add(new IPEndPoint(address, 6969));

endpoints.AddRange(new[] { new IPEndPoint(IPAddress.Any, 6969), new IPEndPoint(IPAddress.IPv6Any, 6969) });

var server = new Server(endpoints, 5, new ServerSettings { UseEncryption = true, ConnectionPollTimeout = 600000 });

var workingDirectory = @$"{Directory.GetCurrentDirectory()}\Files";
if (!Directory.Exists(workingDirectory)) 
    Directory.CreateDirectory(workingDirectory);

server.OnClientConnected += OnConnect;

server.OnClientDisconnected += OnDisconnect;

server.RegisterMessageHandler<FileRequestMessage>(HandleFileRequest);

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

void OnDisconnect (ServerClient sc, DisconnectionInfo info)
{
    Console.WriteLine($"{sc.LocalEndpoint} disconnected {info.Reason}");
}

async void HandleFileRequest (FileRequestMessage msg, ServerClient c)
{
    switch (msg.RequestType)
    {
        case FileRequestMessage.FileRequestType.Download:
            using (FileStream fs = File.OpenRead(@$"{workingDirectory}\{msg.PathRequest}"))
            {
                await SendFile(fs, c, msg);
            }
            break;
        case FileRequestMessage.FileRequestType.Upload:
            Directory.CreateDirectory(@$"{workingDirectory}\{msg.PathRequest}");
            using (FileStream fs = File.Create(@$"{workingDirectory}\{msg.PathRequest}\{msg.FileName}"))
            {
                await fs.WriteAsync(msg.FileData);
                var tree = GetTree(workingDirectory);
                tree.Value = "Root";
                await c.SendObjectAsync(tree);
                Console.WriteLine($"{c.RemoteEndpoint} uploaded {msg.FileName}");
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

async Task SendFile(FileStream file, ServerClient client, FileRequestMessage msg)
{
    const int sendChunkSize = 16384;

    Console.WriteLine($"{client.RemoteEndpoint} requested {msg.PathRequest.Split('\\')[^1]}");

    var id = Guid.NewGuid();
    if (file.Length <= sendChunkSize)
    {
        var newMsg = new FileRequestMessage()
        {
            RequestType = FileRequestMessage.FileRequestType.Upload,
            FileName = msg.PathRequest.Split('\\')[^1],
            RequestId = id, 
            EndOfMessage = true
        };
        newMsg.FileData = new byte[file.Length];
        await file.ReadAsync(newMsg.FileData);
        await client.SendMessageAsync(newMsg);
        return;
    }
    byte[] bytes = new byte[sendChunkSize];
    var max = Math.Ceiling(((float)file.Length) / (float)sendChunkSize);
    for (int i = 0; i < max; i++)
    {
        var newMsg = new FileRequestMessage()
        {
            RequestType = FileRequestMessage.FileRequestType.Upload,
            FileName = msg.PathRequest.Split('\\')[^1],
            RequestId = id,
            EndOfMessage = i == max - 1 ? true : false
        };
        if (i != max - 1)
        {
            newMsg.FileData = bytes;
            await file.ReadAsync(newMsg.FileData);
            await client.SendMessageAsync(newMsg);
        }
        else
        {
            newMsg.FileData = new byte[file.Length - file.Position];
            await file.ReadAsync(newMsg.FileData);
            await client.SendMessageAsync(newMsg);
        }
    }
}