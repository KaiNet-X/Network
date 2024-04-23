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
        this.server.OnMessage<FileRequestMessage>(HandleFileRequest);
    }

    public async Task SendFile(FileStream file, ServerClient client, FileRequestMessage msg)
    {
        const int sendChunkSize = 16384;
        msg.PathRequest = msg.PathRequest.PathFormat();

        Console.WriteLine($"{client.RemoteEndpoint} requested {msg.PathRequest.Split(Path.DirectorySeparatorChar)[^1]}");

        var id = Guid.NewGuid();
        if (file.Length <= sendChunkSize)
        {
            var newMsg = new FileRequestMessage()
            {
                RequestType = FileRequestType.Upload,
                FileName = msg.PathRequest.Split(Path.DirectorySeparatorChar)[^1],
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
                FileName = msg.PathRequest.Split(Path.DirectorySeparatorChar)[^1],
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
                        var path = @$"{workingDirectory}\{msg.User.UserName}\{msg.PathRequest}".PathFormat();
                        var key = await authService.GetUserKeyAsync(msg.User.UserName, msg.User.Password);
                        await CryptoServices.DecryptFileAsync(path, key, key);
                        using (FileStream fs = File.OpenRead(path))
                        {
                            await SendFile(fs, c, msg);
                        }
                        var tree = GetTree(@$"{workingDirectory}\{msg.User.UserName}".PathFormat());
                        tree.Value = "Root";
                        await c.SendObjectAsync(tree);
                        File.Delete(path);
                    }
                    break;
                case FileRequestType.Upload:
                    {
                        Directory.CreateDirectory(@$"{workingDirectory}\{msg.User.UserName}\{msg.PathRequest}".PathFormat());
                        var path = $@"{workingDirectory}\{msg.User.UserName}\{msg.PathRequest}\{msg.FileName}".PathFormat();
                        await using (FileStream fs = File.Create(path))
                        {
                            await fs.WriteAsync(msg.FileData);
                        }
                        var key = await authService.GetUserKeyAsync(msg.User.UserName, msg.User.Password);
                        await CryptoServices.EncryptFileAsync(path, key, key);
                        var tree = GetTree(@$"{workingDirectory}\{msg.User.UserName}".PathFormat());
                        tree.Value = "Root";
                        await c.SendObjectAsync(tree);
                        Console.WriteLine($"{c.RemoteEndpoint} uploaded {msg.FileName}");
                    }
                    break;
                case FileRequestType.Delete:
                    {
                        File.Delete(@$"{workingDirectory}\{msg.User.UserName}\{msg.PathRequest}.aes".PathFormat());
                        var tree = GetTree(@$"{workingDirectory}\{msg.User.UserName}".PathFormat());
                        tree.Value = "Root";
                        await c.SendObjectAsync(tree);
                        Console.WriteLine($"{c.RemoteEndpoint} deleted {msg.PathRequest}");
                    }
                    break;
                case FileRequestType.Tree:
                    {
                        var tree = GetTree(@$"{workingDirectory}\{msg.User.UserName}".PathFormat());
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
        dir = dir.PathFormat();
        var tree = new Tree()
        {
            Nodes = new List<Tree>()
        };

        foreach (var file in Directory.EnumerateFiles(dir))
            tree.Nodes.Add(new Tree() { Value = file.Split(Path.DirectorySeparatorChar)[^1].Replace(".aes", "") });

        foreach (var folder in Directory.EnumerateDirectories(dir))
        {
            var tr = GetTree(folder);
            tr.Value = folder.Split(Path.DirectorySeparatorChar)[^1];
            tree.Nodes.Add(tr);
        }
        return tree;
    }

}
