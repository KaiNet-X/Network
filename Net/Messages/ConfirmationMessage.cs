namespace Net.Messages;

using MessagePack;

[Attributes.RegisterMessageAttribute]
class ConfirmationMessage : MpMessage
{
    public ConfirmationMessage(string @for)
    {
        Content = MessagePackSerializer.Serialize(@for, ResolveOptions);
    }

    public ConfirmationMessage() { }
    protected internal override object GetValue()
    {
        return MessagePackSerializer.Deserialize<string>(Content);
    }
}
