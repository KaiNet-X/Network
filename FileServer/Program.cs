using FileServer;
using Net;
using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.IO;
using System.Net;

// NOTE: This doesn't work for large files. For that, you would have to send the file in multiple segments and reassemble it on the client

var authService = new AuthService();
await authService.LoadUsersAsync();
await authService.AddUser("Kai", "Kai");

var workingDirectory = @$"{Directory.GetCurrentDirectory()}\Files";
if (!Directory.Exists(workingDirectory))
    Directory.CreateDirectory(workingDirectory);

var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName());
var endpoints = new List<IPEndPoint>();

foreach (var address in addresses)
    endpoints.Add(new IPEndPoint(address, 6969));

endpoints.AddRange(new[] { new IPEndPoint(IPAddress.Any, 6969), new IPEndPoint(IPAddress.IPv6Any, 6969) });

var server = new Server(endpoints, new ServerSettings { UseEncryption = true, ConnectionPollTimeout = 600000, MaxClientConnections = 5 });

server.OnClientConnected += OnConnect;

server.OnClientDisconnected += OnDisconnect;

server.RegisterMessageHandler<FileRequestMessage>(HandleFileRequest);

server.Start();

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
        tree.Nodes.Add(new Tree() { Value = file.Split('\\')[^1].Replace(".aes", "") });

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
    if (!await authService.CheckUserAsync(msg.User.UserName, msg.User.Password))
        return;

    try
    {
        switch (msg.RequestType)
        {
            case FileRequestType.Download:
                {
                    var path = @$"{workingDirectory}\{msg.PathRequest}";
                    var key = await authService.GetUserKeyAsync(msg.User.UserName, msg.User.Password);
                    await CryptoServices.DecryptFileAsync(path, key, key);
                    using (FileStream fs = File.OpenRead(path))
                    {
                        await SendFile(fs, c, msg);
                    }
                    File.Delete(path);
                }
                break;
            case FileRequestType.Upload:
                {
                    Directory.CreateDirectory(@$"{workingDirectory}\{msg.PathRequest}");
                    var path = @$"{workingDirectory}\{msg.PathRequest}\{msg.FileName}";
                    await using (FileStream fs = File.Create(path))
                    {
                        await fs.WriteAsync(msg.FileData);
                    }
                    var key = await authService.GetUserKeyAsync(msg.User.UserName, msg.User.Password);
                    await CryptoServices.EncryptFileAsync(path, key, key);
                    var tree = GetTree(workingDirectory);
                    tree.Value = "Root";
                    await c.SendObjectAsync(tree);
                    Console.WriteLine($"{c.RemoteEndpoint} uploaded {msg.FileName}");
                }
                break;
            case FileRequestType.Delete:
                {
                    File.Delete(@$"{workingDirectory}\{msg.PathRequest}");
                    var tree = GetTree(workingDirectory);
                    tree.Value = "Root";
                    await c.SendObjectAsync(tree);
                    Console.WriteLine($"{c.RemoteEndpoint} deleted {msg.PathRequest}");
                }
                break;
            case FileRequestType.Tree:
                {
                    var tree = GetTree(workingDirectory);
                    tree.Value = "Root";
                    await c.SendObjectAsync(tree);
                }
                break;
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
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
            RequestType = FileRequestType.Upload,
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
            RequestType = FileRequestType.Upload,
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