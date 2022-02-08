using System;

namespace Net.Messages
{
    public class CreateChannelMessage : MessageBase
    {
        public override string MessageType => "channel";
        public Guid Id { get; set; }
        public int Port { get; set; }

        public CreateChannelMessage(Guid guid, int port)
        {
            RegisterMessage<CreateChannelMessage>();
            Id = guid;
            Port = port;
        }

        protected internal override object GetValue()
        {
            return Id;
        }
    }
}
