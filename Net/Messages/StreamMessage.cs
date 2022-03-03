using System;

namespace Net.Messages
{
    class StreamMessage : MpMessage
    {
        public StreamMessage() { }
        protected internal override object GetValue()
        {
            throw new NotImplementedException();
        }
    }
}