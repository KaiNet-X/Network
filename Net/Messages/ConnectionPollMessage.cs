namespace Net.Messages
{
    public class ConnectionPollMessage : MessageBase
    {
        public override string MessageType { get; } = "ConnectionPoll";
        public PollMessage PollState { get; set; }

        public enum PollMessage
        {
            SYN,
            ACK
        }
    }
}