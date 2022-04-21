namespace Net.Messages;

using MessagePack;
using System;

[Attributes.RegisterMessageAttribute]
public class ObjectMessage : MpMessage
{
    public override string MessageType => GetType().Name;
    public string TypeName { get; set; }

    public ObjectMessage(object obj)
    {
        //RegisterMessage<ObjectMessage>();
        //RegisterMessage();
        Type t = obj.GetType();
        TypeName = t.Name;
        Content = MessagePackSerializer.Serialize(t, obj, ResolveOptions);
    }
    public ObjectMessage() { }
    protected internal override object GetValue()
    {
        return GetValue(Utilities.GetTypeFromName(TypeName));
    }
}
