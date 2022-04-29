namespace Net.Messages;

[Attributes.RegisterMessageAttribute]
public class ConnectionPollMessage : MessageBase
{
    public PollMessage PollState { get; set; }

    public enum PollMessage
    {
        SYN,
        ACK,
        DISCONNECT
    }
}