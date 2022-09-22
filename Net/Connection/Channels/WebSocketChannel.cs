namespace Net.Connection.Channels;

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketChannel : IChannel
{
    private WebSocket Socket;
    private ArraySegment<byte> bytes;

    public WebSocketChannel(WebSocket socket)
    {
        Socket = socket;
    }

    public bool Connected => throw new NotImplementedException();

    public void Close()
    {
        CloseAsync().GetAwaiter().GetResult();
    }

    public async Task CloseAsync()
    {
        await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
    }

    public byte[] ReceiveBytes()
    {
        return ReceiveBytesAsync().GetAwaiter().GetResult();
    }

    public async Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        await Socket.ReceiveAsync(bytes, token);
        return bytes.ToArray();
    }

    public void SendBytes(byte[] data)
    {
        SendBytesAsync(data).GetAwaiter().GetResult();
    }

    public async Task SendBytesAsync(byte[] data, CancellationToken token = default)
    {
        await Socket.SendAsync(data, WebSocketMessageType.Binary, true, token);
    }
}
