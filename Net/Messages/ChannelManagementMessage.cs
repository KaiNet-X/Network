namespace Net.Messages;

using Attributes;
using System;
using System.Collections.Generic;

[RegisterMessage]
public sealed class ChannelManagementMessage : MessageBase
{
    public Dictionary<string, string> Info { get; set; }
    public string Type { get; set; }
    public byte[] Crypto { get; set; }
    public ChannelManagementMessage() { }
}