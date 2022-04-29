namespace Net.Messages;

using MessagePack;
using System;

[Attributes.RegisterMessageAttribute]
public class ObjectMessage : MpMessage
{
    public string TypeName { get; set; }

    public ObjectMessage(object obj)
    {
        Type t = obj.GetType();
        TypeName = t.Name;
        Content = MessagePackSerializer.Serialize(t, obj, ResolveOptions);
    }

    public ObjectMessage() { }

    protected internal override object GetValue() =>
        GetValue(Utilities.GetTypeFromName(TypeName));
}