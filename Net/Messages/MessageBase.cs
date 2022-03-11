using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Net.Messages
{
    public class MessageBase
    {
        private static Dictionary<string, Type> Registered { get; set; } = new Dictionary<string, Type>();
        public virtual string MessageType { get; set; }

        public static MessageBase Deserialize(byte[] obj)
        {
            string str = Encoding.UTF8.GetString(obj);
            MessageBase msg = JsonSerializer.Deserialize<MessageBase>(str);

            Type t = Registered[msg.MessageType];

            return JsonSerializer.Deserialize(str, t) as MessageBase;
        }

        protected void RegisterMessage()
        {
            Type type = GetType();
            if (!Registered.ContainsKey(type.Name)) Registered[MessageType] = type;
        }

        internal protected virtual List<byte> Serialize() =>
            new List<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, GetType())));

        internal protected virtual Task<List<byte>> SerializeAsync() =>
            Task.FromResult(Serialize());
    }
}