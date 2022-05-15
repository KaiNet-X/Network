namespace Net.Connection.Channels;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class Channel : IChannel
{
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private bool _connected = false;
    private IPEndPoint _remoteEndpoint;
    private bool _disposedValue = false;
    private readonly UdpClient _udp;
    private List<byte[]> _sendBytes = new List<byte[]>();

    public readonly Guid Id;
    public byte[] AesKey;
    public IPEndPoint LocalEndpoint => _udp.Client.LocalEndPoint as IPEndPoint;
    public IPEndPoint RemoteEndpoint => _remoteEndpoint;
    public bool Disposed => _disposedValue;
    public bool Connected 
    {
        get => _connected;
        set
        {
            _connected = value;
            if (value)
            { 
                while (_sendBytes.Count > 0)
                {
                    SendBytes(_sendBytes[0]);
                    _sendBytes.RemoveAt(0);
                }
            }
        }
    }

    public Channel(IPAddress localAddr, IPEndPoint remote, Guid? id = null)
    {
        this.Id = id??Guid.NewGuid();
        _udp = new UdpClient(new IPEndPoint(localAddr, 0));
        _udp.Connect(remote);
        _remoteEndpoint = remote;
    }

    public Channel(IPAddress localAddr, Guid? id = null)
    {
        this.Id = id ?? Guid.NewGuid();
        _udp = new UdpClient(new IPEndPoint(localAddr, 0));
    }

    public void SendBytes(byte[] data)
    {
        if (!Connected)
        {
            _sendBytes.Add(data);
            return;
        }
        data = AesKey == null ? data : CryptoServices.EncryptAES(data, AesKey);
        Utilities.ConcurrentAccess(() => _udp.Send(data, data.Length), _semaphore);
    }
     
    public byte[] RecieveBytes()
    {
        byte[] buffer = null;
        Utilities.ConcurrentAccess(() => buffer = _udp.Receive(ref _remoteEndpoint), _semaphore);
        return AesKey == null ? buffer : CryptoServices.DecryptAES(buffer, AesKey);
    }

    public async Task SendBytesAsync(byte[] data, CancellationToken token = default)
    {
        while (!Connected)
        {
            if (token.IsCancellationRequested) return;
            await Task.Delay(10);
        }

        data = AesKey == null ? data : CryptoServices.EncryptAES(data, AesKey);
        await Utilities.ConcurrentAccessAsync(async (ct) => await _udp.SendAsync(new ReadOnlyMemory<byte>(data), token), _semaphore);
    }

    public async Task<byte[]> RecieveBytesAsync(CancellationToken token = default)
    {
        byte[] buffer = null;
        await Utilities.ConcurrentAccessAsync(async (ct) => buffer = (await _udp.ReceiveAsync(token)).Buffer, _semaphore);

        if (token.IsCancellationRequested) return null;
        return AesKey == null ? buffer : CryptoServices.DecryptAES(buffer, AesKey);
    }

    public void SetRemote(IPEndPoint remote)
    {
        _remoteEndpoint = remote;
        _udp.Connect(_remoteEndpoint);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _udp.Close();
                _semaphore.Dispose();
            }
            _disposedValue = true;
        }
    }

    ~Channel()
    {
        Dispose(disposing: true);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}