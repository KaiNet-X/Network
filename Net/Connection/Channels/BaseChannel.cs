namespace Net.Connection.Channels;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for all channel types. Channels are meant to send raw data from one endpoint to another without interfering with the main connection
/// </summary>
public abstract class BaseChannel
{
    /// <summary>
    /// Shorthand for ConnectionInfo.ConnectedTask
    /// </summary>
    public bool Connected { get; protected set; }

    /// <summary>
    /// Exception that caused the connection to close, if applicable
    /// </summary>
    public Exception ConnectionException { get; set; }

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public abstract void SendBytes(byte[] data);

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public abstract void SendBytes(ReadOnlySpan<byte> data);

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    /// <param name="token"></param>
    public abstract Task SendBytesAsync(byte[] data, CancellationToken token = default);

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    /// <param name="token"></param>
    public abstract Task SendBytesAsync(ReadOnlyMemory<byte> data, CancellationToken token = default);

    /// <summary>
    /// Receive bytes on from remote host
    /// </summary>
    /// <returns>bytes</returns>
    public abstract byte[] ReceiveBytes();

    /// <summary>
    /// Receive bytes on from remote host
    /// </summary>
    /// <returns>bytes</returns>
    public abstract Task<byte[]> ReceiveBytesAsync(CancellationToken token = default);

    /// <summary>
    /// Recieves to a buffer instead of the usual method. This can be used as an optimimization or to mimic the way sockets receive bytes.
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <returns>bytes received</returns>
    public abstract int ReceiveToBuffer(byte[] buffer);

    /// <summary>
    /// Recieves to a buffer instead of the usual method. This can be used as an optimimization or to mimic the way sockets receive bytes.
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <param name="token"></param>
    /// <returns>bytes received</returns>
    public abstract Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default);

    /// <summary>
    /// Recieves to a buffer instead of the usual method. This can be used as an optimimization or to mimic the way sockets receive bytes.
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <returns>bytes received</returns>
    public abstract int ReceiveToBuffer(Span<byte> buffer);

    /// <summary>
    /// Recieves to a buffer instead of the usual method. This can be used as an optimimization or to mimic the way sockets receive bytes.
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <param name="token"></param>
    /// <returns>bytes received</returns>
    public abstract Task<int> ReceiveToBufferAsync(Memory<byte> buffer, CancellationToken token = default);
}