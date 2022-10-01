namespace UnitTests;

using Net.Connection.Channels;
using System;
using System.Threading;
using System.Threading.Tasks;

internal class DummyChannel : IChannel
{
    public bool Connected => true;

    public void Close()
    {
        
    }

    public Task CloseAsync()
    {
        return null;
    }

    public byte[] ReceiveBytes()
    {
        return null;
    }

    public Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        return null;
    }

    public int ReceiveToBuffer(byte[] buffer)
    {
        throw new NotImplementedException();
    }

    public Task<int> ReceiveToBufferAsync(byte[] buffer)
    {
        throw new NotImplementedException();
    }

    public void SendBytes(byte[] data)
    {
        throw new NotImplementedException();
    }

    public Task SendBytesAsync(byte[] data, CancellationToken token = default)
    {
        return null;
    }
}