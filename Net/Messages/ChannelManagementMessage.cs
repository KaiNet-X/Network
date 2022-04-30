namespace Net.Messages;

using System;

[Attributes.RegisterMessageAttribute]
public class ChannelManagementMessage : MessageBase
{
    public Guid Id { get; set; }
    public int Port { get; set; }
    public Mode ManageMode { get; set; }

    public ChannelManagementMessage(Guid guid, int port, Mode mode)
    {
        Id = guid;
        Port = port;
        ManageMode = mode;
    }

    public ChannelManagementMessage(Guid guid, Mode mode)
    {
        Id = guid;
        ManageMode = mode;
    }

    public ChannelManagementMessage() { }

    public enum Mode
    {
        Create,
        Confirm,
        Close
    }
}