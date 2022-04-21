namespace Net.Connection.Channels;

using System;
using System.Threading.Tasks;

public interface IChannel : IDisposable
{
    public void SendBytes(byte[] data);
    public Task SendBytesAsync(byte[] data);
    public byte[] RecieveBytes();
    public Task<byte[]> RecieveBytesAsync();
}