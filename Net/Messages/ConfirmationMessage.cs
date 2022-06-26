namespace Net.Messages;

using Attributes;
using MessagePack;

[RegisterMessageAttribute]
sealed class ConfirmationMessage : MpMessage
{
    public ConfirmationMessage(Confirmation conf)
    {
        Content = MessagePackSerializer.Serialize(conf, ResolveOptions);
    }

    public ConfirmationMessage() { }

    protected internal override object GetValue()
    {
        return MessagePackSerializer.Deserialize<Confirmation>(Content);
    }

    public enum Confirmation
    {
        ENCRYPTION,
        RESOLVED
    }
}
