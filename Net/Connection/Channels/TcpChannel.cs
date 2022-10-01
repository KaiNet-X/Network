using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Connection.Channels;

public class TcpChannel : IChannel
{
    public Socket Socket;

    public bool Connected => throw new NotImplementedException();

    public TcpChannel(Socket socket)
    {
        Socket = socket;
    }

    public void Close()
    {
        Socket.Close();
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
            received = Socket.Receive(buffer, System.Net.Sockets.SocketFlags.None);
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
            received = await Socket.ReceiveAsync(buffer, System.Net.Sockets.SocketFlags.None);
            allBytes.AddRange(buffer.Take(received));
        }
        while (received == buffer_length);

        return allBytes.ToArray();
    }

    public void SendBytes(byte[] data) =>
        Socket.Send(data);

    public async Task SendBytesAsync(byte[] data, CancellationToken token = default) =>
        await Socket.SendAsync(data, SocketFlags.None);

    public int ReceiveToBuffer(byte[] buffer) =>
        Socket.Receive(buffer, SocketFlags.None);

    public async Task<int> ReceiveToBufferAsync(byte[] buffer) =>
        await Socket.ReceiveAsync(buffer, SocketFlags.None);
}