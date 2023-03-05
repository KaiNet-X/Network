namespace Net.Connection.Channels;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class EncryptedTcpChannel : IChannel
{
    internal Socket Socket;
    private byte[] _aesKey;
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
        _aesKey = aesKey;
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
        throw new NotImplementedException();
    }

    public Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public int ReceiveToBuffer(byte[] buffer)
    {
        throw new NotImplementedException();
    }

    public Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public void SendBytes(byte[] data)
    {
        CryptoServices.EncryptAES(data, _aesKey, _aesKey);
        throw new NotImplementedException();
    }

    public Task SendBytesAsync(byte[] data, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
