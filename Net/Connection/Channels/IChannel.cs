namespace Net.Connection.Channels;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for all channel types. Channels are meant to send raw data from one endpoint to another without interfering with the main connection
/// </summary>
public interface IChannel
{
    /// <summary>
    /// Check if channel is connected
    /// </summary>
    public bool Connected { get; }

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
    public byte[] ReceiveBytes();

    /// <summary>
    /// Receive bytes on from remote
    /// </summary>
    /// <returns>bytes</returns>
    public Task<byte[]> ReceiveBytesAsync(CancellationToken token = default);

    /// <summary>
    /// Recieves to a buffer instead of the usual method. This can be used as an optimimization or to mimic the way sockets receive bytes.
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <returns></returns>
    public int ReceiveToBuffer(byte[] buffer);

    /// <summary>
    /// Recieves to a buffer instead of the usual method. This can be used as an optimimization or to mimic the way sockets receive bytes.
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <returns></returns>
    public Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default);

    /// <summary>
    /// Closes the channel
    /// </summary>
    public void Close();

    /// <summary>
    /// Closes the channel
    /// </summary>
    public Task CloseAsync();
}