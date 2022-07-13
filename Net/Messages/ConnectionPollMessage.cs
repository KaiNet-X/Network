namespace Net.Messages;

using Attributes;

[RegisterMessage]
public sealed class ConnectionPollMessage : MessageBase
{
    public PollMessage PollState { get; set; }

    public enum PollMessage
    {
        SYN,
        ACK,
        DISCONNECT
    }
}