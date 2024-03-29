﻿namespace Net.Connection.Channels;

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
public class EncryptedTcpChannel : BaseChannel
{
    private readonly CryptographyService _crypto;
    private CancellationTokenSource cts = new CancellationTokenSource();
    private CancellationToken ct;
    private List<byte> _received = new List<byte>();
    private List<byte> _queued = new List<byte>();

    internal Socket Socket;

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
    /// <param name="crypto"></param>
    public EncryptedTcpChannel(Socket socket, CryptographyService crypto)
    {
        ct = cts.Token;
        _crypto = crypto;
        Socket = socket;
        Remote = socket.RemoteEndPoint as IPEndPoint;
        Local = socket.LocalEndPoint as IPEndPoint;
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
        if (!Connected) return;

        using var source = CancellationTokenSource.CreateLinkedTokenSource(cts != null ? [token, ct] : [token]);

        ReadOnlyMemory<byte> encrypted = _crypto.EncryptAES(data);
        ReadOnlyMemory<byte> head = BitConverter.GetBytes(encrypted.Length);
        Memory<byte> buffer = new byte[head.Length + encrypted.Length];

        head.CopyTo(buffer);
        encrypted.CopyTo(buffer.Slice(4));

        await Socket.SendAsync(buffer, SocketFlags.None, source.Token);
    }

    /// <summary>
    /// Receive bytes on from remote host
    /// </summary>
    /// <returns>bytes</returns>
    public override byte[] ReceiveBytes()
    {
        ReadOnlySpan<byte> Process(int length)
        {
            ReadOnlySpan<byte> back = CollectionsMarshal.AsSpan(_received);
            byte[] data = back.Slice(4, length).ToArray();
            _received.RemoveRange(0, length + 4);
            return _crypto.DecryptAES(data.AsSpan());
        }

        if (!Connected) return Array.Empty<byte>();
        
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
        while (length == -1 || _received.Count < length + 4 && Connected);

        return Process(length).ToArray();
    }

    /// <summary>
    /// Receive bytes on from remote host
    /// </summary>
    /// <returns>bytes</returns>
    public override async Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        byte[] Process(int length)
        {
            ReadOnlySpan<byte> back = CollectionsMarshal.AsSpan(_received);
            byte[] data = back.Slice(4, length).ToArray();
            _received.RemoveRange(0, length + 4);
            return _crypto.DecryptAES(data);
        }

        if (!Connected) return Array.Empty<byte>();
        
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cts != null ? [token, ct] : [token]);

        byte[] buffer = new byte[1024];

        int length = -1;

        if (_received.Count > 0)
        {
            if (length == -1 && _received.Count >= 4)
                length = BitConverter.ToInt32(_received.GetRange(0, 4).ToArray());
            if (_received.Count >= length + 4)
            {
                return Process(length);
            }
        }

        do
        {
            var receiveLength = await Socket.ReceiveAsync(buffer, SocketFlags.None, source.Token);
            _received.AddRange(buffer[..receiveLength]);
            if (length == -1 && _received.Count >= 4)
                length = BitConverter.ToInt32(_received.GetRange(0, 4).ToArray());
        }
        while (length == -1 || _received.Count < length + 4 && Connected);

        return Process(length);
    }

    /// <summary>
    /// This method is not implemented
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public override int ReceiveToBuffer(byte[] buffer) =>
        ReceiveToBuffer(buffer.AsSpan());

    /// <summary>
    /// This method is not implemented
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public override int ReceiveToBuffer(Span<byte> buffer)
    {
        if (buffer.Length == 0 || !Connected) return 0;

        ReadOnlySpan<byte> queueSpan = CollectionsMarshal.AsSpan(_queued);

        var written1 = WriteToSpan(queueSpan, buffer);

        _queued.RemoveRange(0, written1);
        if (written1 == buffer.Length)
            return written1;

        var remaining = buffer.Slice(written1);

        var v = ReceiveBytes();

        var written2 = WriteToSpan(v, remaining);

        if (written2 < v.Length)
            _queued.AddRange(v.AsSpan(written2).ToArray());

        return written1 + written2;
    }

    /// <summary>
    /// This method is not implemented
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="token"></param>
    public override Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default) =>
        ReceiveToBufferAsync(buffer.AsMemory(), token);

    /// <summary>
    /// This method is not implemented
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="token"></param>
    public override async Task<int> ReceiveToBufferAsync(Memory<byte> buffer, CancellationToken token = default)
    {
        if (buffer.Length == 0 || !Connected) return 0;

        using var source = CancellationTokenSource.CreateLinkedTokenSource(cts != null ? [token, ct] : [token]);

        var written1 = WriteToSpan(CollectionsMarshal.AsSpan(_queued), buffer.Span);

        _queued.RemoveRange(0, written1);
        if (written1 == buffer.Length)
            return written1;

        var remaining = buffer.Slice(written1);

        var v = await ReceiveBytesAsync(source.Token);

        var written2 = WriteToSpan(v, remaining.Span);

        if (written2 < v.Length)
            _queued.AddRange(v.AsSpan(written2).ToArray());

        return written1 + written2;
    }

    private int WriteToSpan(ReadOnlySpan<byte> source, Span<byte> dest)
    {
        if (dest.Length == 0) return 0;

        if (source.Length <= dest.Length)
        {
            source.CopyTo(dest);
            return source.Length;
        }

        source.Slice(0, dest.Length).CopyTo(dest);

        return dest.Length;
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
        if (!Connected) return;

        cts.Cancel();
        Socket.Close();
        Connected = false;
        cts.Dispose();
    }
}