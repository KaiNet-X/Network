using Net.JsonResolvers;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Serialization;

public class JSerializer : ISerializer
{
    private JsonSerializerOptions _options = new JsonSerializerOptions();

    private static JSerializer instance;
    public static JSerializer Instance { get => instance ??= new JSerializer(); }

    private JSerializer()
    {
        _options.Converters.Add(new RSAContractResolver());
    }

    public object Deserialize(ReadOnlySpan<byte> bytes, Type type) =>
        JsonSerializer.Deserialize(bytes, type, _options);

    public object Deserialize(byte[] bytes, Type type) =>
        Deserialize((ReadOnlySpan<byte>)bytes, type);

    public async Task<object> DeserializeAsync(byte[] bytes, Type type, CancellationToken token = default)
    {
        using var memStream = new MemoryStream(bytes);
        return await JsonSerializer.DeserializeAsync(memStream, type, _options, token);
    }

    public async Task<object> DeserializeAsync(ReadOnlyMemory<byte> bytes, Type type, CancellationToken token = default)
    {
        using var memStream = new MemoryStream();
        memStream.WriteAsync(bytes, token);
        return await JsonSerializer.DeserializeAsync(memStream, type, _options, token);
    }

    public byte[] Serialize(object obj, Type type) =>
        JsonSerializer.SerializeToUtf8Bytes(obj, type, _options);
    
    public async Task<byte[]> SerializeAsync(object obj, Type type, CancellationToken token = default)
    {
        using var memStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(memStream, obj, type, _options, token);
        return memStream.ToArray();
    }
}
