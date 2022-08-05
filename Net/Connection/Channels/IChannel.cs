namespace Net.Connection.Channels;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for all channel types. Channels are meant to send raw data from one endpoint to another
/// </summary>
public interface IChannel
{
    /// <summary>
    /// Send bytes to remote
    /// </summary>
    /// <param name="data"></param>
    public void SendBytes(byte[] data);

    /// <summary>
    /// Send bytes to remote
    /// </summary>
    /// <param name="data"></param>
    public Task SendBytesAsync(byte[] data, CancellationToken token = default);

    /// <summary>
    /// Receive bytes on from remote
    /// </summary>
    /// <returns>bytes</returns>
    public byte[] RecieveBytes();

    /// <summary>
    /// Receive bytes on from remote
    /// </summary>
    /// <returns>bytes</returns>
    public Task<byte[]> RecieveBytesAsync(CancellationToken token = default);

    /// <summary>
    /// Closes the channel
    /// </summary>
    public void Close();

    /// <summary>
    /// Closes the channel
    /// </summary>
    public Task CloseAsync();
}