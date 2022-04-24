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

server.OnClientDisconnected += delegate (ServerClient sc, bool g)
{
    Console.WriteLine($"{sc.LocalEndpoint} disconnected {(g ? "gracefully" : "ungracefully")}");
};

server.CustomMessageHandlers.Add(FileRequestMessage.Type, async (msg, c) => 
{
    var fMsg = msg as FileRequestMessage;

    switch (fMsg.RequestType)
    {
        case FileRequestMessage.FileRequestType.Download:
            using (FileStream fs = File.OpenRead(@$"{workingDirectory}\{fMsg.PathRequest}"))
            {
                var newMsg = new FileRequestMessage() { RequestType = FileRequestMessage.FileRequestType.Upload, FileName = fMsg.PathRequest.Split('\\')[^1]};
                newMsg.FileData = new byte[fs.Length];
                await fs.ReadAsync(newMsg.FileData);
                await c.SendMessageAsync(newMsg);
            }
            break;
        case FileRequestMessage.FileRequestType.Upload:
            using (FileStream fs = File.Create(@$"{workingDirectory}\{fMsg.PathRequest}"))
            {
                await fs.WriteAsync(fMsg.FileData);
            }
            break;
        case FileRequestMessage.FileRequestType.Tree:
            var tree = GetTree(workingDirectory);
            tree.Value = "Root";
            await c.SendObjectAsync(tree);
            break;
    }
});

await server.StartServerAsync();

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