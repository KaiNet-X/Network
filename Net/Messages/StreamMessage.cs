namespace Net.Messages;

using System;

class StreamMessage : MpMessage
{
    public StreamMessage() { }
    protected internal override object GetValue()
    {
        throw new NotImplementedException();
    }
}
