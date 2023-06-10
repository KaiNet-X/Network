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
    private static MpSerializer instance;
    public static MpSerializer Instance { get => instance ??= new MpSerializer(); }

    private MpSerializer()
    {

    }

    public object Deserialize(ReadOnlyMemory<byte> bytes, Type type) =>
        MessagePackSerializer.Deserialize(type, bytes, Options);

    public object Deserialize(byte[] bytes, Type type) =>
        Deserialize((ReadOnlyMemory<byte>)bytes, type);

    public object Deserialize(ReadOnlySpan<byte> bytes, Type type) =>
        Deserialize(bytes.ToArray(), type);

    public async Task<object> DeserializeAsync(byte[] bytes, Type type, CancellationToken token = default)
    {
        using (var memStream = new MemoryStream(bytes))
            return await MessagePackSerializer.DeserializeAsync(type, memStream, Options);
    }

    public async Task<object> DeserializeAsync(ReadOnlyMemory<byte> bytes, Type type, CancellationToken token = default)
    {
        using var memStream = new MemoryStream();
        await memStream.WriteAsync(bytes);
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
