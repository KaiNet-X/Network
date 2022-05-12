# RegisterMessageAttribute
Put this attribute on any custom message types you create so the library can recognize it. Custom messages can derive from MpMessage or MessageBase

#### Example usage:

This is the implementation of ObjectMessage which derives MpMessage -> MessageBase
~~~c#
namespace Net.Messages;

using Attributes;
using MessagePack;
using System;

[RegisterMessageAttribute]
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
~~~