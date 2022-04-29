using Net.Attributes;
using Net.Messages;

namespace FileClient;

[RegisterMessage]
internal class FileRequestMessage : MessageBase
{
    public FileRequestType RequestType { get; set; }
    public string FileName { get; set; }
    public byte[] FileData { get; set; }
    public string PathRequest { get; set; }

    public enum FileRequestType
    {
        Download,
        Upload,
        Tree
    }
}