namespace Net.Messages;

using Attributes;
using Serialization;
using System;
using System.Reflection;

public sealed class ObjectMessage : MessageBase
{
    public string TypeName { get; set; }
    public byte[] Data { get; set; }
    public static ISerializer DefaultSerializer = MpSerializer.Instance;

    public ObjectMessage(object obj)
    {
        Type t = obj.GetType();
        var alias = t.GetCustomAttribute<NetAliasAttribute>();
        TypeName = alias?.TypeAlias ?? t.Name;
        Data = DefaultSerializer.Serialize(obj, t);
    }

    public ObjectMessage() { }
}