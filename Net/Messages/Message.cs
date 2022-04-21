namespace Net.Messages;

using MessagePack;
using MessagePack.Resolvers;
using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class MpMessage : MessageBase
{
    public virtual byte[] Content { get; init; }

    public MpMessage() { }

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
