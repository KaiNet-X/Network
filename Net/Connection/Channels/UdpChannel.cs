namespace Net.Connection.Channels;

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class UdpChannel : IChannel, IDisposable
{
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private UdpClient _udp;
    private ConcurrentQueue<byte> _byteQueue = new();
    private Task receiver;
    private byte[] _aes;
    private CancellationTokenSource _cts = new CancellationTokenSource();

    public bool Connected { get; private set; }
    public IPEndPoint Remote { get; private set; }
    public IPEndPoint Local { get; private set; }

    public UdpChannel(IPEndPoint local)
    {
        _udp = new UdpClient(local);
        Local = (IPEndPoint)_udp.Client.LocalEndPoint;
    }

    public UdpChannel(IPEndPoint local, byte[] aesKey) : this(local)
    {
        _aes = aesKey;
    }

    public byte[] RecieveBytes()
    {
        var buffer = new byte[_byteQueue.Count];
        Utilities.ConcurrentAccess(() =>
        {
            for (int i = 0; i < buffer.Length; i++)
                if (_byteQueue.TryDequeue(out byte b))
                    buffer[i] = b;
                else if (_byteQueue.Count > 0)
                    i--;

        }, _semaphore);

        return buffer;
    }

    public Task<byte[]> RecieveBytesAsync(CancellationToken token = default)
    {
        return Task.FromResult(RecieveBytes());
    }

    public void SendBytes(byte[] data)
    {
        if (_aes != null)
            data = CryptoServices.EncryptAES(data, _aes, _aes);
        
        Utilities.ConcurrentAccess(() => _udp.Send(data, data.Length), _semaphore);
    }

    public async Task SendBytesAsync(byte[] data, CancellationToken token = default)
    {
        if (_aes != null)
            data = await CryptoServices.EncryptAESAsync(data, _aes, _aes);

        await Utilities.ConcurrentAccessAsync(async (ct) => await _udp.SendAsync(data, token), _semaphore);
    }

    public void SetRemote(IPEndPoint endpoint)
    {
        _udp.Connect(Remote = endpoint);
        Connected = true;
        receiver = ReceiveLoop(_cts.Token);
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        while (Connected)
        {
            if (ct.IsCancellationRequested)
                return;

            var result = await _udp.ReceiveAsync(ct);
            if (_aes == null)
                foreach (byte b in result.Buffer)
                    _byteQueue.Enqueue(b);
            else
            {
                var decrypted = await CryptoServices.DecryptAESAsync(result.Buffer, _aes, _aes);
                foreach (byte b in decrypted)
                    _byteQueue.Enqueue(b);
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _byteQueue.Clear();
        _semaphore.Dispose();
    }
}