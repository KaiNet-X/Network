namespace Net.Messages;

using Attributes;
using Net.Serialization;
using System;

[RegisterMessage]
public sealed class ObjectMessage : MessageBase
{
    public string TypeName { get; set; }
    public byte[] Data { get; set; }
    public static ISerializer DefaultSerializer = MpSerializer.Instance;

    public ObjectMessage(object obj)
    {
        Type t = obj.GetType();
        TypeName = t.Name;
        Data = DefaultSerializer.Serialize(obj, t);
    }

    public ObjectMessage() { }

    internal object GetValue() =>
        DefaultSerializer.Deserialize(Data, Utilities.GetTypeFromName(TypeName));
}