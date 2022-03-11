﻿using MessagePack;

namespace Net.Messages
{
    [Attributes.RegisterMessageAttribute]
    class ConfirmationMessage : MpMessage
    {
        public override string MessageType => GetType().Name;
        public ConfirmationMessage(string @for)
        {
            Content = MessagePackSerializer.Serialize(@for, ResolveOptions);
        }

        public ConfirmationMessage() { }
        protected internal override object GetValue()
        {
            return MessagePackSerializer.Deserialize<string>(Content);
        }
    }
}
