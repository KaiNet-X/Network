namespace UnitTests;

using Net.Connection.Channels;
using System;
using System.Threading;
using System.Threading.Tasks;

internal class DummyChannel : BaseChannel
{
    public DummyChannel()
    {
        Connected = true;
    }
    public override byte[] ReceiveBytes()
    {
        return null;
    }

    public override Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        return null;
    }

    public override int ReceiveToBuffer(byte[] buffer)
    {
        throw new NotImplementedException();
    }

    public override int ReceiveToBuffer(Span<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public override Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public override Task<int> ReceiveToBufferAsync(Memory<byte> buffer, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public override void SendBytes(byte[] data)
    {
        throw new NotImplementedException();
    }

    public override void SendBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public override Task SendBytesAsync(byte[] data, CancellationToken token = default)
    {
        return null;
    }

    public override Task SendBytesAsync(ReadOnlyMemory<byte> data, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    protected void Close()
    {
        throw new NotImplementedException();
    }
}