namespace Net.Connection.Channels;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class UdpChannel : IChannel, IDisposable
{
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private UdpClient _udp;
    private ConcurrentQueue<byte> _byteQueue = new();
    private List<byte> _byteList = new();
    private Task receiver;
    private byte[] _aes;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private TaskCompletionSource tcs = new TaskCompletionSource();

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
        byte[] buffer = new byte[0];

        tcs.Task.Wait();

        Utilities.ConcurrentAccess(() =>
        {
            tcs = new TaskCompletionSource();
            //for (int i = 0; i < buffer.Length; i++)
            //    if (_byteQueue.TryDequeue(out byte b))
            //        buffer[i] = b;
            //    else if (_byteQueue.Count > 0)
            //        i--;
            buffer = _byteList.ToArray();
            _byteList.Clear();
        }, _semaphore);

        return buffer;
    }

    public async Task<byte[]> RecieveBytesAsync(CancellationToken token = default)
    {
        byte[] buffer = new byte[0];

        await tcs.Task;

        await Utilities.ConcurrentAccessAsync((ct) =>
        {
            tcs = new TaskCompletionSource();
            //for (int i = 0; i < buffer.Length; i++)
            //    if (_byteQueue.TryDequeue(out byte b))
            //        buffer[i] = b;
            //    else if (_byteQueue.Count > 0)
            //        i--;
            buffer = _byteList.ToArray();
            _byteList.Clear();
            return Task.CompletedTask;
        }, _semaphore);

        return buffer;
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
        receiver = Task.Run(async () => await ReceiveLoop(_cts.Token));
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        var receiveTask = _udp.ReceiveAsync(ct);
        while (Connected)
        {
            if (ct.IsCancellationRequested)
                return;

            var result = await receiveTask;
            receiveTask = _udp.ReceiveAsync(ct);
            if (_aes == null)
                Utilities.ConcurrentAccess(() =>
                {
                    //foreach (byte b in result.Buffer)
                    //    _byteQueue.Enqueue(b);
                    _byteList.AddRange(result.Buffer);
                    tcs.SetResult();
                }, _semaphore);
            else
            {
                var decrypted = await CryptoServices.DecryptAESAsync(result.Buffer, _aes, _aes);
                Utilities.ConcurrentAccess(() =>
                {
                    //foreach (byte b in decrypted)
                    //    _byteQueue.Enqueue(b);
                    _byteList.AddRange(decrypted);
                    tcs.SetResult();
                }, _semaphore);
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