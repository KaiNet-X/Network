using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
        byte[] buffer = new byte[1024];
        var received = Socket.Receive(buffer, SocketFlags.None);
        return buffer;
    }

    public async Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        byte[] buffer = new byte[1024];
        var received = await Socket.ReceiveAsync(buffer, SocketFlags.None, token);
        return buffer;
    }

    public void SendBytes(byte[] data) =>
        Socket.Send(data);

    public async Task SendBytesAsync(byte[] data, CancellationToken token = default) =>
        await Socket.SendAsync(data, SocketFlags.None);
}
