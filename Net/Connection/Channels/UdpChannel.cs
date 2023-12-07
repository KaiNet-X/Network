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
public class UdpChannel : IChannel, IDisposable
{
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private UdpClient _udp;
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
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public void SendBytes(byte[] data) => 
        SendBytes(data.AsSpan());

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public void SendBytes(ReadOnlySpan<byte> data)
    {
        if (!Connected)
            return;

        _semaphore.Wait();

        try
        {
            _udp.Send(data);
        }
        finally
        {
            _semaphore?.Release();
        }
    }

    /// <summary>
    /// Sends data to the remote channel
    /// </summary>
    /// <param name="data"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task SendBytesAsync(byte[] data, CancellationToken token = default) =>
        SendBytesAsync(data.AsMemory(), token);

    /// <summary>
    /// Sends data to the remote channel
    /// </summary>
    /// <param name="data"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task SendBytesAsync(ReadOnlyMemory<byte> data, CancellationToken token = default)
    {
        using var t = CancellationTokenSource.CreateLinkedTokenSource(_cts != null ? [token, _cts.Token] : [token]);

        if (!Connected) return;

        await Utilities.ConcurrentAccessAsync(async (ct) => await _udp.SendAsync(data, t.Token), _semaphore);
    }

    /// <summary>
    /// Receive bytes
    /// </summary>
    /// <returns></returns>
    public byte[] ReceiveBytes()
    {
        if (!Connected) return Array.Empty<byte>();

        var endpoint = Remote;

        try
        {
            return _udp.Receive(ref endpoint);
        }
        catch (ObjectDisposedException)
        {
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Receive bytes
    /// </summary>
    /// <returns></returns>
    public async Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        if (!Connected) return Array.Empty<byte>();
        
        using var t = CancellationTokenSource.CreateLinkedTokenSource(_cts != null ? [token, _cts.Token] : [token]);

        try
        {
            var result = await _udp.ReceiveAsync(t.Token);
            return result.Buffer;
        }
        catch (ObjectDisposedException)
        {
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Recieves to a buffer from the socket
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <returns>bytes received</returns>
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

    /// <summary>
    /// Recieves to a buffer from the socket
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <returns>bytes received</returns>
    public int ReceiveToBuffer(Span<byte> buffer)
    {
        if (!Connected) return 0;

        try
        {
            return _udp.Client.Receive(buffer, SocketFlags.None);
        }
        catch (ObjectDisposedException)
        {
            return 0;
        }
    }


    /// <summary>
    /// Recieves to a buffer from the socket
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <param name="token"></param>
    /// <returns>bytes received</returns>
    public async Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default)
    {
        if (!Connected) return 0;

        using var t = CancellationTokenSource.CreateLinkedTokenSource(_cts != null ? [token, _cts.Token] : [token]);

        try
        {
            return await _udp.Client.ReceiveAsync(buffer, SocketFlags.None, t.Token);
        }
        catch (ObjectDisposedException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Recieves to a buffer from the socket
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <param name="token"></param>
    /// <returns>bytes received</returns>
    public async Task<int> ReceiveToBufferAsync(Memory<byte> buffer, CancellationToken token = default)
    {
        if (!Connected) return 0;

        using var t = CancellationTokenSource.CreateLinkedTokenSource(_cts != null ? [token, _cts.Token] : [token]);

        try
        {
            return await _udp.Client.ReceiveAsync(buffer, t.Token);
        }
        catch (ObjectDisposedException)
        {
            return 0;
        }
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

    /// <summary>
    /// Closes the channel. Handled by the client it is associated with.
    /// </summary>
    public void Dispose()
    {
        Connected = false;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
        _semaphore.Dispose();
        _udp.Close();
    }
}