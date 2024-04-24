namespace FileServer;

using Net.Messages;

internal class FileRequestMessage : MessageBase
{
    private string _fileName;
    private string _directory;

    public FileRequestType RequestType { get; init; }
    public Guid RequestId { get; init; }
    public User User { get; init; }
    public bool EndOfMessage { get; init; }
    public byte[] FileData { get; set; }
    public string PathRequest { get; init; }

    public string Directory
    {
        get
        {
            return _directory ??= Path.GetDirectoryName(PathRequest);
        }
    }
    public string FileName
    {
        get
        {
            return _fileName ??= Path.GetFileName(PathRequest);
        }
    }
}

public enum FileRequestType
{
    Download,
    Upload,
    Delete,
    Tree
}