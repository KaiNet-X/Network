namespace Net.Connection.Channels;

using System;
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
    private CancellationTokenSource _cts = new CancellationTokenSource();

    /// <summary>
    /// Check if channel is connected
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
    /// Receive bytes from internal queue
    /// </summary>
    /// <returns></returns>
    public byte[] ReceiveBytes()
    {
        if (!Connected || _cts.IsCancellationRequested)
            return null;

        var endpoint = Remote;

        try
        {
            return _udp.Receive(ref endpoint);  
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Receive bytes from internal queue
    /// </summary>
    /// <returns></returns>
    public async Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        using var t = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token);

        if (!Connected || t.IsCancellationRequested)
            return null;

        try
        {
            var result = await _udp.ReceiveAsync(t.Token);
            return result.Buffer;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }

    public void SendBytes(byte[] data)
    {
        if (!Connected || _cts.IsCancellationRequested)
            return;
        
        Utilities.ConcurrentAccess(() => _udp.SendAsync(data, data.Length), _semaphore);
    }

    public async Task SendBytesAsync(byte[] data, CancellationToken token = default)
    {
        using var t = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token);

        if (!Connected || t.IsCancellationRequested)
            return;

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
        if (!Connected || _cts.IsCancellationRequested)
            return 0;

        try
        {
            return _udp.Client.Receive(buffer, SocketFlags.None);
        }
        catch (ObjectDisposedException)
        {
            return 0;
        }
    }

    public async Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default)
    {
        if (!Connected || _cts.IsCancellationRequested)
            return 0;

        try
        {
            return await _udp.Client.ReceiveAsync(buffer, SocketFlags.None, token);
        }
        catch (ObjectDisposedException)
        {
            return 0;
        }
    }
}