namespace Net.Messages;

using Attributes;

[RegisterMessage]
public sealed class ChannelManagementMessage : MessageBase
{
    public int Port { get; set; }
    public int? IdPort { get; set; }
    public byte[] Aes { get; set; }
    public Mode ManageMode { get; set; }

    public ChannelManagementMessage(int port, Mode mode, byte[] aes = null)
    {
        Port = port;
        ManageMode = mode;
        Aes = aes;
    }

    public ChannelManagementMessage(int port, Mode mode, int idPort)
    {
        Port = port;
        ManageMode = mode;
        IdPort = idPort;
    }

    public ChannelManagementMessage() { }

    public enum Mode
    {
        Create,
        Confirm,
        Close
    }
}