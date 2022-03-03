using MessagePack;
using MessagePack.Resolvers;
using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Net.Messages
{
    public class MpMessage : MessageBase
    {
        public virtual byte[] Content { get; init; }

        public MpMessage() { }

        //public override string MessageType { get; set; }
        [JsonIgnore]
        public virtual MessagePackSerializerOptions ResolveOptions => ContractlessStandardResolver.Options;

        internal virtual object GetValue(Type t) =>
            MessagePackSerializer.Deserialize(t, Content, ResolveOptions);

        internal virtual async Task<object> GetValueAsync(Type t) =>
            await MessagePackSerializer.DeserializeAsync(t, new MemoryStream(Content), ResolveOptions);

        internal protected virtual object GetValue()
        {
            return null;
        }

        internal protected virtual Task<object> GetValueAsync() => Task.FromResult(GetValue());
    }
}