namespace Net.Messages;

public sealed class ConfirmationMessage : MessageBase
{
    public Confirmation Confirm { get; set; }

    public ConfirmationMessage(Confirmation conf)
    {
        Confirm = conf;
    }

    public ConfirmationMessage() { }

    public enum Confirmation
    {
        ENCRYPTION,
        RESOLVED
    }
}
