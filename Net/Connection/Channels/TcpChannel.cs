namespace Net.Connection.Channels;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A channel that communicates using TCP
/// </summary>
public class TcpChannel : BaseChannel
{
    protected internal Socket Socket;
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    /// <summary>
    /// Remote endpoint
    /// </summary>
    public IPEndPoint Remote { get; private set; }

    /// <summary>
    /// Local endpoint
    /// </summary>
    public IPEndPoint Local { get; private set; }

    /// <summary>
    /// Opens a tcp channel on an already connected socket
    /// </summary>
    /// <param name="socket"></param>
    public TcpChannel(Socket socket)
    {
        Socket = socket;
        Remote = Socket.RemoteEndPoint as IPEndPoint;
        Local = Socket.LocalEndPoint as IPEndPoint;
        Connected = true;
    }

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public override void SendBytes(byte[] data) =>
        SendBytes(data.AsSpan());

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public override void SendBytes(ReadOnlySpan<byte> data)
    {
        if (!Connected) return;
        
        try
        {
            Socket.Send(data);
        }
        catch (SocketException e)
        {
            ChannelError(e);
        }
    }

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    /// <param name="token"></param>
    public override Task SendBytesAsync(byte[] data, CancellationToken token = default) =>
        SendBytesAsync(data.AsMemory(), token);

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    /// <param name="token"></param>
    public override async Task SendBytesAsync(ReadOnlyMemory<byte> data, CancellationToken token = default)
    {        
        try
        {
            if (!Connected) return;

            using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource != null ? [token, cancellationTokenSource.Token] : [token]);

            await Socket.SendAsync(data, SocketFlags.None, source.Token);
        }
        catch (SocketException e)
        {
            ChannelError(e);
        }
    }

    /// <summary>
    /// Receive bytes on from remote host
    /// </summary>
    /// <returns>bytes</returns>
    public override byte[] ReceiveBytes()
    {
        if (!Connected) return Array.Empty<byte>();
        
        List<byte> allBytes = new List<byte>();
        const int buffer_length = 1024;
        byte[] buffer = new byte[buffer_length];

        int received;
        do
        {
            try
            {
                received = Socket.Receive(buffer, SocketFlags.None);
                allBytes.AddRange(buffer[..received]);
            }
            catch (SocketException e)
            {
                ChannelError(e);
                return Array.Empty<byte>();
            }
        }
        while (received == buffer_length && Connected);

        return allBytes.ToArray();
    }

    /// <summary>
    /// Receive bytes on from remote host
    /// </summary>
    /// <returns>bytes</returns>
    public override async Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        if (!Connected) return Array.Empty<byte>();

        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource != null ? [token, cancellationTokenSource.Token] : [token]);

        List<byte> allBytes = new List<byte>();
        const int buffer_length = 1024;
        byte[] buffer = new byte[buffer_length];

        int received = 0;
        do
        {
            try
            {
                received = await Socket.ReceiveAsync(buffer, SocketFlags.None, source.Token);
                allBytes.AddRange(buffer[..received]);
            }
            catch (SocketException e)
            {
                ChannelError(e);
                return Array.Empty<byte>();
            }
        }
        while (received == buffer_length && Connected);

        return allBytes.ToArray();
    }

    /// <summary>
    /// Recieves to a buffer, calling the underlying socket method.
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <returns></returns>
    public override int ReceiveToBuffer(byte[] buffer) => ReceiveToBuffer(buffer.AsSpan());

    /// <summary>
    /// Recieves to a buffer, calling the underlying socket method.
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <returns></returns>
    public override int ReceiveToBuffer(Span<byte> buffer)
    {
        if (!Connected) return 0;

        try
        {
            return Socket.Receive(buffer, SocketFlags.None);
        }
        catch (SocketException e)
        {
            ChannelError(e);
            return 0;
        }
    }

    /// <summary>
    /// Recieves to a buffer, calling the underlying socket method.
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public override async Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default) =>
        await ReceiveToBufferAsync(buffer.AsMemory(), token);

    /// <summary>
    /// Recieves to a buffer, calling the underlying socket method.
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public override async Task<int> ReceiveToBufferAsync(Memory<byte> buffer, CancellationToken token = default)
    {
        if (!Connected) return 0;

        try
        {        
            using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource != null ? [token, cancellationTokenSource.Token] : [token]);

            return await Socket.ReceiveAsync(buffer, token);
        }
        catch (SocketException e)
        {
            ChannelError(e);
            return 0;
        }
    }

    private void ChannelError(Exception e)
    {
        ConnectionException = e;
        Close();
    }

    /// <summary>
    /// Closes the channel. Handled by the client it is associated with
    /// </summary>
    protected internal void Close()
    {
        cancellationTokenSource.Cancel();
        Socket.Close();
        Connected = false;
        cancellationTokenSource.Dispose();
    }
}