using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Net.Messages
{
    public abstract class MessageBase
    {
        private static Dictionary<string, Type> Registered { get; set; } = new Dictionary<string, Type>();
        public static void RegisterMessage<TMessage>(string name) where TMessage : MessageBase
        {
            Type type = typeof(TMessage);
            if (!Registered.ContainsKey(type.Name)) Registered[name] = type;
        }

        public static MessageBase Deserialize(byte[] obj)
        {
            string str = Encoding.UTF8.GetString(obj);
            MessageBase msg = JsonSerializer.Deserialize<Message>(str);

            Type t = Registered[msg.MessageType];

            return JsonSerializer.Deserialize(str, t) as MessageBase;
        }

        [JsonIgnore] 
        public virtual MessagePackSerializerOptions ResolveOptions => ContractlessStandardResolver.Options;

        public virtual string MessageType { get; set; }
        public virtual byte[] Content { get; init; }

        protected void RegisterMessage<TMessage>() where TMessage : MessageBase
        {
            Type type = typeof(TMessage);
            if (!Registered.ContainsKey(type.Name)) Registered[MessageType] = type;
        }
        internal protected virtual List<byte> Serialize()
        {
            return new List<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, GetType())));
        }
        internal protected virtual Task<List<byte>> SerializeAsync() 
        {
            return Task.FromResult(Serialize());
        }
        internal protected abstract object GetValue();
        internal protected virtual Task<object> GetValueAsync() => Task.FromResult(GetValue());

        internal virtual object GetValue(Type t)
        {
            return MessagePackSerializer.Deserialize(t, Content, ResolveOptions);
        }
        internal virtual async Task<object> GetValueAsync(Type t)
        {
            return await MessagePackSerializer.DeserializeAsync(t, new MemoryStream(Content), ResolveOptions);
        }
    }
}