using MessagePack;

namespace Net.Messages
{
    class ConfirmationMessage : MpMessage
    {
        public override string MessageType => "confirmation";
        public ConfirmationMessage(string @for)
        {
            Content = MessagePackSerializer.Serialize(@for, ResolveOptions);
            //RegisterMessage<ConfirmationMessage>();
            RegisterMessage();
        }
        public ConfirmationMessage() { }
        protected internal override object GetValue()
        {
            return MessagePackSerializer.Deserialize<string>(Content);
        }
    }
}
