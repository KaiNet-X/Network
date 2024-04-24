namespace FileServer;

using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

internal class FileService
{
    private readonly TcpServer server;
    private readonly AuthService authService;
    private readonly string workingDirectory;
    private readonly ConcurrentDictionary<Guid, Stream> OpenFiles = new();

    public FileService(TcpServer server, AuthService authService, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(authService);

        this.server = server;
        this.authService = authService;
        this.workingDirectory = workingDirectory;
        this.server.OnMessage<FileRequestMessage>(HandleFileRequest);
    }

    public async Task SendFile(Stream file, ServerClient client, FileRequestMessage msg)
    {
        const int sendChunkSize = 16384;

        var fileName = msg.PathRequest.PathFormat().Split(Path.DirectorySeparatorChar)[^1];

        var id = Guid.NewGuid();

        byte[] bytes = new byte[file.Length <= sendChunkSize ? file.Length : sendChunkSize];
        var max = Math.Ceiling(((float)file.Length) / (float)sendChunkSize);

        for (int i = 0; i < max; i++)
        {
            var eom = i == max - 1;

            var newMsg = new FileRequestMessage()
            {
                RequestType = FileRequestType.Upload,
                FileName = fileName,
                RequestId = id,
                EndOfMessage = eom,
                FileData = eom ? (i > 0 ? new byte[file.Length - file.Position] : bytes) : bytes
            };

            await file.ReadAsync(newMsg.FileData);
            await client.SendMessageAsync(newMsg);
        }
    }

    async void HandleFileRequest(FileRequestMessage msg, ServerClient c)
    {
        var key = await authService.GetUserKeyAsync(msg.User.UserName, msg.User.Password);

        if (key.Length == 0)
        {
            Console.WriteLine($"Authentication error on {c.RemoteEndpoint.Address}, name: {msg.User.UserName}");
            return;
        }

        var dir = @$"{workingDirectory}\{msg.User.UserName}\{msg.PathRequest}".PathFormat();

        if (dir.Contains($"..{Path.DirectorySeparatorChar}"))
        {
            Console.WriteLine($"Potential malicious url from {c.RemoteEndpoint.Address}");
            Console.WriteLine(dir);
            return;
        }
        var fpath = @$"{dir}\{msg.FileName}".PathFormat();

        try
        {
            switch (msg.RequestType)
            {
                case FileRequestType.Download:
                    {
                        Console.WriteLine($"{c.RemoteEndpoint} requested {msg.FileName}");
                        Directory.CreateDirectory(@$"{workingDirectory}\temp".PathFormat());
                        var tempPath = @$"{workingDirectory}\temp\{msg.RequestId}.tmp".PathFormat();
                        await using (FileStream destination = File.Create(tempPath))
                        {
                            await using (FileStream source = File.OpenRead($"{fpath}.aes"))
                            {
                                await CryptoServices.DecryptStreamAsync(source, destination, key, key);
                            }
                            await SendFile(destination, c, msg);
                        }
                        File.Delete(tempPath);
                        Console.WriteLine($"{c.RemoteEndpoint} downloaded {msg.FileName}");
                    }
                    break;
                case FileRequestType.Upload:
                    {
                        Directory.CreateDirectory(dir);
                        await using (MemoryStream source = new MemoryStream(msg.FileData))
                        {
                            await using (FileStream destination = File.Create($"{fpath}.aes"))
                            {
                                await CryptoServices.EncryptStreamAsync(source, destination, key, key);
                            }
                        }
                        Console.WriteLine($"{c.RemoteEndpoint} uploaded {msg.FileName}");
                    }
                    break;
                case FileRequestType.Delete:
                    {
                        File.Delete(@$"{dir}.aes".PathFormat());
                        Console.WriteLine($"{c.RemoteEndpoint} deleted {msg.PathRequest}");
                    }
                    break;
                case FileRequestType.Tree:
                    {
                        Directory.CreateDirectory(dir);
                    }
                    break;
            }
            await SendTree(c, msg.User.UserName);

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

    private Task SendTree(ServerClient c, string username) {
        var tree = GetTree(@$"{workingDirectory}\{username}".PathFormat());
        tree.Value = "Root";
        return c.SendObjectAsync(tree);
    }
}
