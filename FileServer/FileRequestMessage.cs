namespace FileServer;

using Net.Messages;

internal class FileRequestMessage : MessageBase
{
    public FileRequestType RequestType { get; set; }
    public Guid RequestId { get; set; }
    public User User { get; set; }
    public bool EndOfMessage { get; set; }
    public string FileName { get; set; }
    public byte[] FileData { get; set; }
    public string PathRequest { get; set; }
}

public enum FileRequestType
{
    Download,
    Upload,
    Delete,
    Tree
}