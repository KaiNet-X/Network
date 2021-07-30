using System;
using System.Threading.Tasks;

namespace Net.Messages
{
    public class Message : MessageBase
    {
        public Message() { }

        public override string MessageType { get; set; }

        protected internal override Task<object> GetValue()
        {
            throw new NotImplementedException();
        }
    }
}
