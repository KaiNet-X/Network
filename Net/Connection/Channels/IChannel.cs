namespace Net.Connection.Channels;

using System.Threading;
using System.Threading.Tasks;

public interface IChannel
{
    public void SendBytes(byte[] data);
    public Task SendBytesAsync(byte[] data, CancellationToken token = default);
    public byte[] RecieveBytes();
    public Task<byte[]> RecieveBytesAsync(CancellationToken token = default);
    public void Close();
}