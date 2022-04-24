using System;
using System.Text.Json.Serialization;

namespace Net.Messages;

internal class Message : TypeAccessorMessage
{
    public Message() { }
    public new string MessageType {get; set;}
}
class TypeAccessorMessage : MessageBase
{
    public override string MessageType => throw new NotImplementedException();
}