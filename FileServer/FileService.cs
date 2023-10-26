namespace FileServer;

using Net.Connection.Clients.NewTcp;
using Net.Connection.Servers;
using System.Threading.Tasks;

internal class FileService
{
    private Server server;

    public FileService(Server server)
    {
        ArgumentNullException.ThrowIfNull(server);
        this.server = server;
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
}
