using System;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Serialization;

public interface ISerializer
{
    public byte[] Serialize(object obj, Type type);
    public object Deserialize(byte[] bytes, Type type);
    public Task<byte[]> SerializeAsync(object obj, Type type, CancellationToken token = default);
    public Task<object> DeserializeAsync(byte[] bytes, Type type, CancellationToken token = default);
}
