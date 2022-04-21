namespace Net.Connection.Channels;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class Channel : IChannel
{
    private bool _connected = false;
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
            //Task.Run(() => 
            //{
            //    while (Connected)
            //        _receiveBlocks.Add(Udp.Receive(ref remoteEndpoint));
            //});
        }
    }
    public byte[] AesKey { get; set; }
    public readonly Guid Id;
    public int Port => (Udp.Client.LocalEndPoint as IPEndPoint).Port;
    private IPEndPoint remoteEndpoint;
    private bool disposedValue;
    private readonly UdpClient Udp;
    private List<byte[]> _sendBytes = new List<byte[]>();
    private List<byte[]> _receiveBlocks = new List<byte[]>();

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
            data = AesKey == null ? data : CryptoServices.EncryptAES(data, AesKey);
            _sendBytes.Add(CryptoServices.DecryptAES(data, AesKey));
            return;
        }
        Udp.Send(data, data.Length);
    }

    public byte[] RecieveBytes()
    {
        byte[] buffer = Udp.Receive(ref remoteEndpoint);
        return AesKey == null ? buffer : CryptoServices.DecryptAES(buffer, AesKey);
    }

    public async Task SendBytesAsync(byte[] data)
    {
        while (!Connected) ;
        data = AesKey == null ? data : CryptoServices.EncryptAES(data, AesKey);
        await Udp.SendAsync(data, data.Length);
        Udp.Dispose();
    }

    public async Task<byte[]> RecieveBytesAsync()
    {
        byte[] buffer = (await Udp.ReceiveAsync()).Buffer;
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