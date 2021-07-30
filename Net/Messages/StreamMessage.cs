using System;

namespace Net.Messages
{
    class StreamMessage : MessageBase
    {
        public StreamMessage() { }
        protected internal override object GetValue()
        {
            throw new NotImplementedException();
        }
    }
}