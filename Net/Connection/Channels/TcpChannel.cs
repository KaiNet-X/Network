namespace Net.Connection.Channels;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A channel that communicates using TCP. Encryption is not yet supported.
/// </summary>
public class TcpChannel : IChannel, IDisposable
{
    protected internal Socket Socket;

    /// <summary>
    /// Check if channel is connected
    /// </summary>
    public bool Connected { get; private set; }

    /// <summary>
    /// Remote endpoint
    /// </summary>
    public IPEndPoint Remote => Socket.RemoteEndPoint as IPEndPoint;

    /// <summary>
    /// Local endpoint
    /// </summary>
    public IPEndPoint Local => Socket.LocalEndPoint as IPEndPoint;

    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    /// <summary>
    /// Opens a tcp channel on an already connected socket
    /// </summary>
    /// <param name="socket"></param>
    public TcpChannel(Socket socket)
    {
        Socket = socket;
        Connected = true;
    }

    /// <summary>
    /// Closes the channel. Handled by the client it is associated with.
    /// </summary>
    public void Dispose()
    {
        Socket.Close();
        Connected = false;
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        cancellationTokenSource = null;
    }

    /// <summary>
    /// Receive bytes on from remote host
    /// </summary>
    /// <returns>bytes</returns>
    public byte[] ReceiveBytes()
    {
        if (!Connected || cancellationTokenSource.IsCancellationRequested) return null;

        List<byte> allBytes = new List<byte>();
        const int buffer_length = 1024;
        byte[] buffer = new byte[buffer_length];

        int received = 0;
        do
        {
            if (cancellationTokenSource.Token.IsCancellationRequested) return null;

            try
            {
                received = Socket.Receive(buffer, SocketFlags.None);
                allBytes.AddRange(buffer[..received]);
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }
        while (received == buffer_length && !cancellationTokenSource.IsCancellationRequested);
        
        return allBytes.ToArray();
    }

    /// <summary>
    /// Receive bytes on from remote host
    /// </summary>
    /// <returns>bytes</returns>
    public async Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource != null ? new [] { token, cancellationTokenSource.Token } : new [] { token });

        if (!Connected || source.IsCancellationRequested) return null;

        List<byte> allBytes = new List<byte>();
        const int buffer_length = 1024;
        byte[] buffer = new byte[buffer_length];

        int received = 0;
        do
        {
            if (source.IsCancellationRequested) return null;

            try
            {
                received = await Socket.ReceiveAsync(buffer, SocketFlags.None, source.Token);
                allBytes.AddRange(buffer[..received]);
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }
        while (received == buffer_length && !source.IsCancellationRequested);

        return allBytes.ToArray();
    }

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public void SendBytes(byte[] data)
    {
        if (!Connected || cancellationTokenSource.IsCancellationRequested) return;

        try
        {
            Socket.Send(data);
        }
        catch(ObjectDisposedException)
        {
            
        }
    }

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public async Task SendBytesAsync(byte[] data, CancellationToken token = default)
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource != null ? new[] { token, cancellationTokenSource.Token } : new[] { token });

        if (!Connected || source.IsCancellationRequested) return;

        try
        {
            await Socket.SendAsync(data, SocketFlags.None, source.Token);
        }
        catch(ObjectDisposedException)
        {
            
        }
    }

    /// <summary>
    /// Recieves to a buffer, calling the underlying socket method.
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <returns></returns>
    public int ReceiveToBuffer(byte[] buffer)
    {
        if (!Connected || cancellationTokenSource.IsCancellationRequested) return 0;

        try
        {
            return Socket.Receive(buffer, SocketFlags.None);
        }
        catch (ObjectDisposedException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Recieves to a buffer, calling the underlying socket method.
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <returns></returns>
    public async Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default)
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource != null ? new[] { token, cancellationTokenSource.Token } : new[] { token });

        if (!Connected || source.IsCancellationRequested) return 0;

        try
        {
            return await Socket.ReceiveAsync(buffer, SocketFlags.None, token);
        }
        catch (ObjectDisposedException)
        {
            return 0;
        }
    }
}