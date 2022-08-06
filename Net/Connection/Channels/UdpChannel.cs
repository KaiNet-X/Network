namespace Net.Connection.Channels;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// This channel is designed to send UDP data between clients. Call SetRemote to connect to a remote endpoing
/// </summary>
public class UdpChannel : IChannel
{
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private UdpClient _udp;
    private List<byte> _byteList = new();
    private Task receiver;
    private byte[] _aes;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private TaskCompletionSource tcs = new TaskCompletionSource();

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
    public byte[] RecieveBytes()
    {
        byte[] buffer = new byte[0];

        tcs.Task.Wait();

        Utilities.ConcurrentAccess(() =>
        {
            tcs = new TaskCompletionSource();
            buffer = _byteList.ToArray();
            _byteList.Clear();
        }, _semaphore);

        return buffer;
    }

    /// <summary>
    /// Receive bytes from internal queue
    /// </summary>
    /// <returns></returns>
    public async Task<byte[]> RecieveBytesAsync(CancellationToken token = default)
    {
        byte[] buffer = new byte[0];

        await tcs.Task;

        await Utilities.ConcurrentAccessAsync((ct) =>
        {
            tcs = new TaskCompletionSource();
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

    /// <summary>
    /// Sets the remote endpoint and connects to it 
    /// </summary>
    /// <param name="endpoint"></param>
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
                    _byteList.AddRange(result.Buffer);
                    tcs.SetResult();
                }, _semaphore);
            else
            {
                var decrypted = await CryptoServices.DecryptAESAsync(result.Buffer, _aes, _aes);
                Utilities.ConcurrentAccess(() =>
                {
                    _byteList.AddRange(decrypted);
                    tcs.SetResult();
                }, _semaphore);
            }
        }
    }

    public void Close()
    {
        _cts.Cancel();
        _semaphore.Dispose();
    }

    public Task CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }
}