using System;
using System.Threading.Tasks;

namespace Net.Connection.Channels;

public interface IChannel : IDisposable
{
    public void SendBytes(byte[] data);
    public Task SendBytesAsync(byte[] data);
    public byte[] RecieveBytes();
    public Task<byte[]> RecieveBytesAsync();
}