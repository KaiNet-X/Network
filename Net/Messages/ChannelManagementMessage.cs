namespace Net.Messages;

using System;

[Attributes.RegisterMessageAttribute]
public class ChannelManagementMessage : MpMessage
{
    public override string MessageType => GetType().Name;
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

    protected internal override object GetValue() => Id;

    public enum Mode
    {
        Create,
        Confirm
    }
}
