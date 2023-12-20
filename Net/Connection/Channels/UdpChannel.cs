namespace Net.Connection.Channels;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// This channel is designed to send UDP data between clients. Call SetRemote to connect to a remote endpoint
/// </summary>
public class UdpChannel : BaseChannel
{
    private UdpClient udp;
    private CancellationTokenSource cts = new CancellationTokenSource();
    private CancellationToken ct;

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
        ct = cts.Token;
        udp = new UdpClient(local);
        Local = (IPEndPoint)udp.Client.LocalEndPoint;
    }

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public override void SendBytes(byte[] data) => 
        SendBytes(data.AsSpan());

    /// <summary>
    /// Send bytes to remote host
    /// </summary>
    /// <param name="data"></param>
    public override void SendBytes(ReadOnlySpan<byte> data)
    {
        if (!Connected) return;

        try
        {
            udp.Send(data);
        }
        catch (SocketException e) 
        {
            ChannelError(e);
        }
    }

    /// <summary>
    /// Sends data to the remote channel
    /// </summary>
    /// <param name="data"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public override Task SendBytesAsync(byte[] data, CancellationToken token = default) =>
        SendBytesAsync(data.AsMemory(), token);

    /// <summary>
    /// Sends data to the remote channel
    /// </summary>
    /// <param name="data"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public override async Task SendBytesAsync(ReadOnlyMemory<byte> data, CancellationToken token = default)
    {
        if (!Connected) return;

        try
        {
            using var source = CancellationTokenSource.CreateLinkedTokenSource(cts != null ? [token, ct] : [token]);

             await udp.SendAsync(data, source.Token);
        }
        catch (SocketException e)
        {
            ChannelError(e);
        }
    }

    /// <summary>
    /// Receive bytes
    /// </summary>
    /// <returns></returns>
    public override byte[] ReceiveBytes()
    {
        if (!Connected) return Array.Empty<byte>();

        try
        {
            var endpoint = Remote;

            return udp.Receive(ref endpoint);
        }
        catch (SocketException e)
        {
            ChannelError(e);
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Receive bytes
    /// </summary>
    /// <returns></returns>
    public override async Task<byte[]> ReceiveBytesAsync(CancellationToken token = default)
    {
        if (!Connected) return Array.Empty<byte>();
        
        try
        {
            using var source = CancellationTokenSource.CreateLinkedTokenSource(cts != null ? [token, ct] : [token]);

            var result = await udp.ReceiveAsync(source.Token);
            return result.Buffer;
        }
        catch (SocketException e)
        {
            ChannelError(e);
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Recieves to a buffer from the socket
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <returns>bytes received</returns>
    public override int ReceiveToBuffer(byte[] buffer)
    {
        if (!Connected) return 0;

        try
        {    
            return udp.Client.Receive(buffer, SocketFlags.None);
        }
        catch (SocketException e)
        {
            ChannelError(e);
            return 0;
        }
    }

    /// <summary>
    /// Recieves to a buffer from the socket
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <returns>bytes received</returns>
    public override int ReceiveToBuffer(Span<byte> buffer)
    {
        if (!Connected) return 0;

        try
        {
            return udp.Client.Receive(buffer, SocketFlags.None);
        }
        catch (SocketException e)
        {
            ChannelError(e);
            return 0;
        }
    }


    /// <summary>
    /// Recieves to a buffer from the socket
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <param name="token"></param>
    /// <returns>bytes received</returns>
    public override async Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default)
    {
        if (!Connected) return 0;

        try
        {
            using var source = CancellationTokenSource.CreateLinkedTokenSource(cts != null ? [token, ct] : [token]);

            return await udp.Client.ReceiveAsync(buffer, SocketFlags.None, source.Token);
        }
        catch (SocketException e)
        {
            ChannelError(e);
            return 0;
        }
    }

    /// <summary>
    /// Recieves to a buffer from the socket
    /// </summary>
    /// <param name="buffer">Buffer to receive to</param>
    /// <param name="token"></param>
    /// <returns>bytes received</returns>
    public override async Task<int> ReceiveToBufferAsync(Memory<byte> buffer, CancellationToken token = default)
    {
        if (!Connected) return 0;

        try
        {
            using var source = CancellationTokenSource.CreateLinkedTokenSource(cts != null ? [token, ct] : [token]);

            return await udp.Client.ReceiveAsync(buffer, source.Token);
        }
        catch (SocketException e)
        {
            ChannelError(e);
            return 0;
        }
    }

    /// <summary>
    /// Sets the remote endpoint and connects to it 
    /// </summary>
    /// <param name="endpoint"></param>
    public void SetRemote(IPEndPoint endpoint)
    {
        udp.Connect(Remote = endpoint);
        Connected = true;
    }

    private void ChannelError(Exception e)
    {
        ConnectionException = e;
        Close();
    }

    /// <summary>
    /// Closes the channel. Handled by the client it is associated with
    /// </summary>
    protected internal void Close()
    {
        if (!Connected) return;

        cts.Cancel();
        udp.Close();
        Connected = false;
        cts.Dispose();
    }
}