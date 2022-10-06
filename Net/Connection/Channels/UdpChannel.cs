namespace Net.Connection.Channels;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// This channel is designed to send UDP data between clients. Call SetRemote to connect to a remote endpoint
/// </summary>
public class UdpChannel : IChannel
{
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private UdpClient _udp;
    private ConcurrentQueue<byte> _byteQueue = new();
    private byte[] _aes;
    private CancellationTokenSource _cts = new CancellationTokenSource();

    /// <summary>
    /// If the channel is connected
    /// </summary>
    public bool Connected { get; private set; }

    /// <summary>
    /// Remote endpoint
    /// </summary>
    public IPEndPoint Remote { get; private set; }

    /// <summary>
    /// Local endpoint
    /// </summary>
    public IPEndPoint Local { get; private set; }

    /// <summary>
    /// Udp channel bound to an endpoint
    /// </summary>
    /// <param name="local"></param>
    public UdpChannel(IPEndPoint local)
    {
        _udp = new UdpClient(local);
        Local = (IPEndPoint)_udp.Client.LocalEndPoint;
    }

    /// <summary>
    /// Udp channel bound to an endpoint using an AES encryption key
    /// </summary>
    /// <param name="local"></param>
    /// <param name="aesKey"></param>
    public UdpChannel(IPEndPoint local, byte[] aesKey) : this(local)
    {
        _aes = aesKey;
    }

    /// <summary>
    /// Receive bytes from internal queue
    /// </summary>
    /// <returns></returns>
    public byte[] ReceiveBytes()
    {
        //if (!Connected || !_byteQueue.TryDequeueRange(out var res))
        //    return null;

        //return res;

        return ReceiveBytesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Receive bytes from internal queue
    /// </summary>
    /// <returns></returns>
    public async Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        using var t = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token);

        var result = await _udp.ReceiveAsync(t.Token);
        if (_aes == null)
            return result.Buffer;
        else
        {
            var decrypted = await CryptoServices.DecryptAESAsync(result.Buffer, _aes, _aes);
            return decrypted;
        }
    }

    public void SendBytes(byte[] data)
    {
        if (!Connected)
            return;

        if (_aes != null)
            data = CryptoServices.EncryptAES(data, _aes, _aes);
        
        Utilities.ConcurrentAccess(() => _udp.SendAsync(data, data.Length), _semaphore);
    }

    public async Task SendBytesAsync(byte[] data, CancellationToken token = default)
    {
        using var t = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token);

        if (!Connected)
            return;

        if (_aes != null)
            data = await CryptoServices.EncryptAESAsync(data, _aes, _aes);

        await Utilities.ConcurrentAccessAsync(async (ct) => await _udp.SendAsync(data, t.Token), _semaphore);
    }

    /// <summary>
    /// Sets the remote endpoint and connects to it 
    /// </summary>
    /// <param name="endpoint"></param>
    public void SetRemote(IPEndPoint endpoint)
    {
        _udp.Connect(Remote = endpoint);
        Connected = true;
    }

    public void Close()
    {
        Connected = false;
        _byteQueue.Clear();
        _byteQueue = null;
        _cts.Cancel();
        _semaphore.Dispose();
        _udp.Close();
    }

    public Task CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }

    public int ReceiveToBuffer(byte[] buffer)
    {
        var ep = Remote as EndPoint;
        return _udp.Client.ReceiveFrom(buffer, SocketFlags.None, ref ep);
    }

    public async Task<int> ReceiveToBufferAsync(byte[] buffer)
    {
        var ep = Remote as EndPoint;
        return (await _udp.Client.ReceiveFromAsync(buffer, SocketFlags.None, ep)).ReceivedBytes;
    }
}