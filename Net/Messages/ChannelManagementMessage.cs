namespace Net.Messages;

using System.Collections.Generic;

public sealed class ChannelManagementMessage : MessageBase
{
    public Dictionary<string, string> Info { get; set; }
    public string Type { get; set; }
    public byte[] Crypto { get; set; }
    public ChannelManagementMessage() { }
}