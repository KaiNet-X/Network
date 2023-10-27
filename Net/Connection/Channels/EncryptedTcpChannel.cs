namespace Net.Connection.Channels;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// This channel sends encrypted data over TCP
/// </summary>
public class EncryptedTcpChannel : IChannel, IDisposable
{
    private readonly CryptographyService _crypto;
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private List<byte> _received = new List<byte>();

    internal Socket Socket;

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

    /// <summary>
    /// Opens a tcp channel on an already connected socket
    /// </summary>
    /// <param name="socket"></param>
    public EncryptedTcpChannel(Socket socket, CryptographyService crypto)
    {
        _crypto = crypto;
        Socket = socket;
        Connected = true;
    }

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public void SendBytes(byte[] data) =>
        SendBytes(data.AsSpan());

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public void SendBytes(ReadOnlySpan<byte> data)
    {
        if (!Connected || cancellationTokenSource.IsCancellationRequested) return;

        ReadOnlySpan<byte> encrypted = _crypto.EncryptAES(data);
        ReadOnlySpan<byte> head = BitConverter.GetBytes(encrypted.Length);
        Span<byte> buffer = new byte[head.Length + encrypted.Length];

        head.CopyTo(buffer);
        encrypted.CopyTo(buffer.Slice(4));

        Socket.Send(buffer);
    }

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public Task SendBytesAsync(byte[] data, CancellationToken token = default) =>
        SendBytesAsync(data.AsMemory(), token);

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public async Task SendBytesAsync(ReadOnlyMemory<byte> data, CancellationToken token = default)
    {
        if (!Connected || cancellationTokenSource.IsCancellationRequested)
            return;

        ReadOnlyMemory<byte> encrypted = _crypto.EncryptAES(data);
        ReadOnlyMemory<byte> head = BitConverter.GetBytes(encrypted.Length);
        Memory<byte> buffer = new byte[head.Length + encrypted.Length];

        head.CopyTo(buffer);
        encrypted.CopyTo(buffer.Slice(4));

        await Socket.SendAsync(buffer, SocketFlags.None, token);
    }

    /// <summary>
    /// Receive bytes on from remote host
    /// </summary>
    /// <returns>bytes</returns>
    public byte[] ReceiveBytes()
    {
        ReadOnlySpan<byte> Process(int length)
        {
            ReadOnlySpan<byte> back = CollectionsMarshal.AsSpan(_received);
            byte[] data = back.Slice(4, length).ToArray();
            _received.RemoveRange(0, length + 4);
            return _crypto.DecryptAES(data.AsSpan());
        }

        if (!Connected || cancellationTokenSource.IsCancellationRequested) 
            return Array.Empty<byte>();

        byte[] buffer = new byte[1024];

        int length = -1;

        if (_received.Count > 0)
        {
            if (length == -1 && _received.Count >= 4)
                length = BitConverter.ToInt32(_received.GetRange(0, 4).ToArray());
            if (_received.Count >= length + 4)
                return Process(length).ToArray();
        }

        do
        {
            var receiveLength = Socket.Receive(buffer, SocketFlags.None);
            _received.AddRange(buffer[..receiveLength]);
            if (length == -1 && _received.Count >= 4)
                length = BitConverter.ToInt32(_received.GetRange(0, 4).ToArray());
        }
        while (length == -1 || _received.Count < length + 4 && !cancellationTokenSource.IsCancellationRequested);

        return Process(length).ToArray();
    }

    /// <summary>
    /// Receive bytes on from remote host
    /// </summary>
    /// <returns>bytes</returns>
    public async Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        byte[] Process(int length)
        {
            ReadOnlySpan<byte> back = CollectionsMarshal.AsSpan(_received);
            byte[] data = back.Slice(4, length).ToArray();
            _received.RemoveRange(0, length + 4);
            return _crypto.DecryptAES(data);
        }

        if (!Connected || cancellationTokenSource.IsCancellationRequested) return Array.Empty<byte>();

        byte[] buffer = new byte[1024];

        int length = -1;

        if (_received.Count > 0)
        {
            if (length == -1 && _received.Count >= 4)
                length = BitConverter.ToInt32(_received.GetRange(0, 4).ToArray());
            if (_received.Count >= length + 4)
                return Process(length);
        }

        do
        {
            await Socket.ReceiveAsync(buffer, SocketFlags.None);
            _received.AddRange(buffer);
            if (length == -1 && _received.Count >= 4)
                length = BitConverter.ToInt32(_received.GetRange(0, 4).ToArray());
        }
        while (length == -1 || _received.Count < length + 4 && !cancellationTokenSource.IsCancellationRequested);

        return Process(length);
    }

    /// <summary>
    /// This method is not implemented
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public int ReceiveToBuffer(byte[] buffer)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// This method is not implemented
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public int ReceiveToBuffer(Span<byte> buffer)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// This method is not implemented
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// This method is not implemented
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<int> ReceiveToBufferAsync(Memory<byte> buffer, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Closes the channel. Handled by the client it is associated with
    /// </summary>
    public void Close()
    {
        Socket.Close();
        Connected = false;
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Closes the channel. Handled by the client it is associated with
    /// </summary>
    public Task CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the channel. Typically only called internally
    /// </summary>
    public void Dispose()
    {
        Socket.Close();
        Connected = false;
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        cancellationTokenSource = null;
    }
}