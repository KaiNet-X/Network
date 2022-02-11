using MessagePack;
using System;

namespace Net.Messages
{
    public class ObjectMessage : MessageBase
    {
        public override string MessageType => "object";
        public string TypeName { get; set; }

        public ObjectMessage(object obj)
        {
            //RegisterMessage<ObjectMessage>();
            RegisterMessage();
            Type t = obj.GetType();
            TypeName = t.Name;
            Content = MessagePackSerializer.Serialize(t, obj, ResolveOptions);
        }
        public ObjectMessage() { }
        protected internal override object GetValue()
        {
            return GetValue(Utilities.GetTypeFromName(TypeName));
        }
    }
}
