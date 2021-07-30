using MessagePack;

namespace Net.Messages
{
    class ConfirmationMessage : MessageBase
    {
        public override string MessageType => "confirmation";
        public ConfirmationMessage(string @for)
        {
            Content = MessagePackSerializer.Serialize(@for, ResolveOptions);
            RegisterMessage<ConfirmationMessage>();
        }
        public ConfirmationMessage() { }
        protected internal override object GetValue()
        {
            return MessagePackSerializer.Deserialize<string>(Content);
        }
    }
}
