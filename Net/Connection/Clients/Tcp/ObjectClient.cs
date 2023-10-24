namespace Net.Connection.Clients.Tcp;

using Channels;
using Net.Connection.Clients.Generic;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

/// <summary>
/// Base client for Client and ServerClient that adds functionality for sending/receiving objects.
/// </summary>
public class ObjectClient : ObjectClient<TcpChannel>
{
    /// <summary>
    /// Gets the local endpoint
    /// </summary>
    public IPEndPoint LocalEndpoint => localEndPoint;

    /// <summary>
    /// Gets the remote endpoint
    /// </summary>
    public IPEndPoint RemoteEndpoint => remoteEndPoint;

    /// <summary>
    /// Local endpoint
    /// </summary>
    protected IPEndPoint localEndPoint;

    /// <summary>
    /// Remote endpoint
    /// </summary>
    protected IPEndPoint remoteEndPoint;

    protected internal volatile List<(IChannel channel, TaskCompletionSource tcs)> _wait = new();

    protected ObjectClient() : base()
    {
        Utilities.RegisterUdpChannel(this, new Lazy<TcpChannel>(() => Connection, true));
        Utilities.RegisterTcpChannel(this, new Lazy<TcpChannel>(() => Connection, true));
        Utilities.RegisterEncryptedTcpChannel(this, new Lazy<TcpChannel>(() => Connection, true));
    }

    private protected override void CloseConnection()
    {
        Connection.Socket.Close();
        Connection = null;
    }
}