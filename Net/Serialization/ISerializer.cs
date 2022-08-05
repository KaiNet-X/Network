using System;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Serialization;

/// <summary>
/// Serializer used in generalclient
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Converts an object to bytes
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public byte[] Serialize(object obj, Type type);

    /// <summary>
    /// Converts bytes to an object
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public object Deserialize(byte[] bytes, Type type);

    /// <summary>
    /// Converts an object to bytes
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public Task<byte[]> SerializeAsync(object obj, Type type, CancellationToken token = default);

    /// <summary>
    /// Converts bytes to an object
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public Task<object> DeserializeAsync(byte[] bytes, Type type, CancellationToken token = default);
}
