namespace Net.Messages;

internal class ConnectionPollMessage : MessageBase
{
    public bool IsResponse;

    public ConnectionPollMessage(bool isResponse)
    {
        IsResponse = isResponse;
    }

    public ConnectionPollMessage() { }
}
