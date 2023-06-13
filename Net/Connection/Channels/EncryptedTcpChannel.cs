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
    internal Socket Socket;
    internal TcpClient Client;

    private CryptographyService _crypto = new CryptographyService();

    //private byte[] _aesKey;

    private List<byte> _received = new List<byte>();

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
    public EncryptedTcpChannel(Socket socket, byte[] aesKey)
    {
        _crypto.AesKey = aesKey;
        Socket = socket;
        Connected = true;
    }

    /// <summary>
    /// Closes the channel. Handled by the client it is associated with.
    /// </summary>
    public void Close()
    {
        Socket.Close();
        Connected = false;
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Closes the channel. Handled by the client it is associated with.
    /// </summary>
    public Task CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }

    public byte[] ReceiveBytes()
    {
        ReadOnlySpan<byte> Process(int length)
        {
            ReadOnlySpan<byte> back = CollectionsMarshal.AsSpan(_received);
            byte[] data = back.Slice(4, length).ToArray();
            _received.RemoveRange(0, length + 4);
            return _crypto.DecryptAES(data.AsSpan(), _crypto.AesKey);
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

    public async Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        byte[] Process(int length)
        {
            ReadOnlySpan<byte> back = CollectionsMarshal.AsSpan(_received);
            byte[] data = back.Slice(4, length).ToArray();
            _received.RemoveRange(0, length + 4);
            return _crypto.DecryptAES(data, _crypto.AesKey);
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

    public int ReceiveToBuffer(byte[] buffer)
    {
        throw new NotImplementedException();
    }

    public Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public void SendBytes(byte[] data) =>
        SendBytes(data.AsSpan());

    public async Task SendBytesAsync(byte[] data, CancellationToken token = default) =>
        SendBytesAsync(data.AsMemory(), token);

    public void Dispose()
    {
        Socket.Close();
        Connected = false;
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        cancellationTokenSource = null;
    }

    public void SendBytes(ReadOnlySpan<byte> data)
    {
        if (!Connected || cancellationTokenSource.IsCancellationRequested) return;

        ReadOnlySpan<byte> encrypted = _crypto.EncryptAES(data, _crypto.AesKey);
        ReadOnlySpan<byte> head = BitConverter.GetBytes(encrypted.Length);
        Span<byte> buffer = new byte[head.Length + encrypted.Length];

        head.CopyTo(buffer);
        encrypted.CopyTo(buffer.Slice(4));

        Socket.Send(buffer);
    }

    public async Task SendBytesAsync(ReadOnlyMemory<byte> data, CancellationToken token = default)
    {
        if (!Connected || cancellationTokenSource.IsCancellationRequested)
            return;

        ReadOnlyMemory<byte> encrypted = _crypto.EncryptAES(data, _crypto.AesKey);
        ReadOnlyMemory<byte> head = BitConverter.GetBytes(encrypted.Length);
        Memory<byte> buffer = new byte[head.Length + encrypted.Length];

        head.CopyTo(buffer);
        encrypted.CopyTo(buffer.Slice(4));

        await Socket.SendAsync(buffer, SocketFlags.None, token);
    }
}
