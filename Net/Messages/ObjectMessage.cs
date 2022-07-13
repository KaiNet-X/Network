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
        Data = Serializer.Serialize(obj, t);
    }

    public ObjectMessage() { }

    internal object GetValue() =>
        Serializer.Deserialize(Data, Utilities.GetTypeFromName(TypeName));
}