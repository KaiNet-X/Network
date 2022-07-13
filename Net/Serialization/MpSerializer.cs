namespace Net.Serialization;

using MessagePack;
using MessagePack.Resolvers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class MpSerializer : ISerializer
{
    public MessagePackSerializerOptions Options { get; set; } = ContractlessStandardResolverAllowPrivate.Options;

    public object Deserialize(byte[] bytes, Type type) =>
        MessagePackSerializer.Deserialize(type, bytes, Options);

    public async Task<object> DeserializeAsync(byte[] bytes, Type type, CancellationToken token = default)
    {
        using (var memStream = new MemoryStream(bytes))
            return await MessagePackSerializer.DeserializeAsync(type, memStream, Options);
    }

    public byte[] Serialize(object obj, Type type) =>
        MessagePackSerializer.Serialize(type, obj, Options);

    public async Task<byte[]> SerializeAsync(object obj, Type type, CancellationToken token = default)
    {
        using var memStream = new MemoryStream();
        await MessagePackSerializer.SerializeAsync(type, memStream, obj, Options, token);
        return memStream.ToArray();
    }
}
