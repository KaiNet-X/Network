namespace FileServer;

using Net.Connection.Clients.Tcp;
using Net.Connection.Servers;
using System.Collections.Concurrent;
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

        Console.WriteLine($"{client.RemoteEndpoint} requested {msg.PathRequest.Split(Path.DirectorySeparatorChar)[^1]}");

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
        if (!await authService.CheckUserAsync(msg.User.UserName, msg.User.Password))
            return;

        var dir = @$"{workingDirectory}\{msg.User.UserName}\{msg.PathRequest}".PathFormat();
        var fpath = @$"{dir}\{msg.FileName}".PathFormat();

        try
        {
            switch (msg.RequestType)
            {
                case FileRequestType.Download:
                    {
                        var key = await authService.GetUserKeyAsync(msg.User.UserName, msg.User.Password);
                        await using Stream fs = await CryptoServices.DecryptedFileStreamAsync(dir, key, key);
                        await SendFile(fs, c, msg);
                        File.Delete(dir);
                        Console.WriteLine($"{c.RemoteEndpoint} downloaded {msg.PathRequest}");
                    }
                    break;
                case FileRequestType.Upload:
                    {
                        Directory.CreateDirectory(dir);
                        var key = await authService.GetUserKeyAsync(msg.User.UserName, msg.User.Password);
                        await CryptoServices.CreateEncryptedFileAsync(fpath, msg.FileData, key, key);
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
