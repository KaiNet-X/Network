﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Connection.Channels;

public class TcpChannel : IChannel
{
    public Socket Socket;

    public bool Connected { get; private set; }

    public IPEndPoint Remote => Socket.RemoteEndPoint as IPEndPoint;

    public IPEndPoint Local => Socket.LocalEndPoint as IPEndPoint;

    private byte[] _aes;

    public TcpChannel(Socket socket, byte[] aes = null)
    {
        Socket = socket;
        Connected = true;
        _aes = aes;
    }

    public void Close()
    {
        Socket.Close();
        Connected = false;
    }

    public Task CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }

    public byte[] ReceiveBytes()
    {
        List<byte> allBytes = new List<byte>();
        const int buffer_length = 1024;
        byte[] buffer = new byte[buffer_length];

        int received;
        do
        {
            received = Socket.Receive(buffer, SocketFlags.None);
            allBytes.AddRange(buffer.Take(received));
        }
        while (received == buffer_length);

        return allBytes.ToArray();
    }

    public async Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        List<byte> allBytes = new List<byte>();
        const int buffer_length = 1024;
        byte[] buffer = new byte[buffer_length];

        int received;
        do
        {
            received = await Socket.ReceiveAsync(buffer, SocketFlags.None, token);
            allBytes.AddRange(buffer[..received]);
        }
        while (received == buffer_length && !token.IsCancellationRequested);

        return allBytes.ToArray();
    }

    public void SendBytes(byte[] data) =>
        Socket.Send(data);

    public async Task SendBytesAsync(byte[] data, CancellationToken token = default) =>
        await Socket.SendAsync(data, SocketFlags.None);

    public int ReceiveToBuffer(byte[] buffer) =>
        Socket.Receive(buffer, SocketFlags.None);

    public async Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default) =>
        await Socket.ReceiveAsync(buffer, SocketFlags.None, token);
}