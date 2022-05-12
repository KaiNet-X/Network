namespace Net.Messages;

using Attributes;
using MessagePack;

[RegisterMessageAttribute]
sealed class ConfirmationMessage : MpMessage
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
