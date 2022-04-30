namespace Net.Connection.Channels;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class Channel : IChannel
{
    private bool _connected = false;
    private IPEndPoint remoteEndpoint;
    private bool disposedValue = false;
    private readonly UdpClient Udp;
    private List<byte[]> _sendBytes = new List<byte[]>();

    public readonly Guid Id;
    public byte[] AesKey { get; set; }
    public int Port => (Udp.Client.LocalEndPoint as IPEndPoint).Port;
    public bool Disposed => disposedValue;
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
        Udp = new UdpClient(new IPEndPoint(localAddr, 0));
        Udp.Connect(remote);
        remoteEndpoint = remote;
    }

    public Channel(IPAddress localAddr, Guid? id = null)
    {
        this.Id = id ?? Guid.NewGuid();
        Udp = new UdpClient(new IPEndPoint(localAddr, 0));
    }

    public void SendBytes(byte[] data)
    {
        if (!Connected)
        {
            _sendBytes.Add(data);
            return;
        }
        data = AesKey == null ? data : CryptoServices.EncryptAES(data, AesKey);
        Udp.Send(data, data.Length);
    }
     
    public byte[] RecieveBytes()
    {
        byte[] buffer = Udp.Receive(ref remoteEndpoint);
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
        await Udp.SendAsync(new ReadOnlyMemory<byte>(data), token);
    }

    public async Task<byte[]> RecieveBytesAsync(CancellationToken token = default)
    {
        byte[] buffer = (await Udp.ReceiveAsync(token)).Buffer;

        if (token.IsCancellationRequested) return null;
        return AesKey == null ? buffer : CryptoServices.DecryptAES(buffer, AesKey);
    }

    public void SetRemote(IPEndPoint remote)
    {
        remoteEndpoint = remote;
        Udp.Connect(remoteEndpoint);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Udp.Dispose();
            }
            disposedValue = true;
        }
    }

    ~Channel()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}