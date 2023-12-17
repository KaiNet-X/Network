namespace UnitTests;

using Net;
using Net.Connection.Channels;
using System;
using System.Threading;
using System.Threading.Tasks;

internal class DummyChannel : IChannel
{
    public bool Connected => true;

    public ChannelConnectionInfo ConnectionInfo => throw new NotImplementedException();

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

    public int ReceiveToBuffer(Span<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public Task<int> ReceiveToBufferAsync(byte[] buffer)
    {
        throw new NotImplementedException();
    }

    public Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> ReceiveToBufferAsync(Memory<byte> buffer, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public void SendBytes(byte[] data)
    {
        throw new NotImplementedException();
    }

    public void SendBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public Task SendBytesAsync(byte[] data, CancellationToken token = default)
    {
        return null;
    }

    public Task SendBytesAsync(ReadOnlyMemory<byte> data, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}