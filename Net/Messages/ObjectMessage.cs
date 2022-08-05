namespace Net.Messages;

using Attributes;
using System;

[RegisterMessage]
public sealed class ObjectMessage : MessageBase
{
    public string TypeName { get; set; }
    public byte[] Data { get; set; }

    public ObjectMessage(object obj)
    {
        Type t = obj.GetType();
        TypeName = t.Name;
        Data = MessageParser.Serializer.Serialize(obj, t);
    }

    public ObjectMessage() { }

    internal object GetValue() =>
        MessageParser.Serializer.Deserialize(Data, Utilities.GetTypeFromName(TypeName));
}