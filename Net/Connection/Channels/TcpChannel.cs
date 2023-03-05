using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Connection.Channels;

/// <summary>
/// A channel that communicates using TCP. Encryption is not yet supported.
/// </summary>
public class TcpChannel : IChannel
{
    public Socket Socket;

    public bool Connected { get; private set; }

    public IPEndPoint Remote => Socket.RemoteEndPoint as IPEndPoint;

    public IPEndPoint Local => Socket.LocalEndPoint as IPEndPoint;

    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    public TcpChannel(Socket socket)
    {
        Socket = socket;
        Connected = true;
    }

    public void Close()
    {
        Socket.Close();
        Connected = false;
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        cancellationTokenSource = null;
    }

    public Task CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }

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