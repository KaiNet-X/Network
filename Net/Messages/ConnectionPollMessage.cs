namespace Net.Messages
{
    [Attributes.RegisterMessageAttribute]
    public class ConnectionPollMessage : MessageBase
    {
        public override string MessageType => GetType().Name;

        public PollMessage PollState { get; set; }

        public enum PollMessage
        {
            SYN,
            ACK,
            DISCONNECT
        }
    }
}