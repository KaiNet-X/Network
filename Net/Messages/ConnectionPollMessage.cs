namespace Net.Messages
{
    public class ConnectionPollMessage : MessageBase
    {
        public override string MessageType => "ConnectionPoll";

        public PollMessage PollState { get; set; }

        public enum PollMessage
        {
            SYN,
            ACK
        }
    }
}