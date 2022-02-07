using System;

namespace Net.Messages
{
    public class CreateChannelMessage : MessageBase
    {
        public override string MessageType => "channel";
        public Guid Id { get; set; }

        public CreateChannelMessage(Guid guid)
        {
            Id = guid;
        }

        protected internal override object GetValue()
        {
            return Id;
        }
    }
}
