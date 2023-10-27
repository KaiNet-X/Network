namespace FileServer;

using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.Threading.Tasks;

internal class FileService
{
    private readonly TcpServer server;
    private readonly AuthService authService;
    private readonly string workingDirectory;

    public FileService(TcpServer server, AuthService authService, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(authService);

        this.server = server;
        this.authService = authService;
        this.workingDirectory = workingDirectory;
        this.server.RegisterMessageHandler<FileRequestMessage>(HandleFileRequest);
    }

    public async Task SendFile(FileStream file, ServerClient client, FileRequestMessage msg)
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

    async void HandleFileRequest(FileRequestMessage msg, ServerClient c)
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
                        await server.SendObjectToAllAsync(tree);
                        Console.WriteLine($"{c.RemoteEndpoint} uploaded {msg.FileName}");
                    }
                    break;
                case FileRequestType.Delete:
                    {
                        File.Delete(@$"{workingDirectory}\{msg.PathRequest}.aes");
                        var tree = GetTree(workingDirectory);
                        tree.Value = "Root";
                        await server.SendObjectToAllAsync(tree);
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

    public Tree GetTree(string dir)
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

}
