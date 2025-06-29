﻿namespace Net.Connection.Clients.Tcp;

using Channels;
using Generic;
using System.Collections.Generic;
using System.Net;
using System;
using System.Threading.Tasks;

/// <summary>
/// Base client for Client and ServerClient that adds functionality for sending/receiving objects.
/// </summary>
public class ObjectClient : ObjectClient<TcpChannel>
{
    /// <summary>
    /// Gets the local endpoint
    /// </summary>
    public IPEndPoint LocalEndpoint { get; protected set; }

    /// <summary>
    /// Gets the remote endpoint
    /// </summary>
    public IPEndPoint RemoteEndpoint { get; protected set; }

    protected internal volatile List<(BaseChannel channel, TaskCompletionSource tcs)> _wait = new();

    protected ObjectClient() : base()
    {
        ChannelHelper.RegisterUdpChannel(this, new Lazy<TcpChannel>(() => Connection, true));
        ChannelHelper.RegisterTcpChannel(this, new Lazy<TcpChannel>(() => Connection, true));
        ChannelHelper.RegisterEncryptedTcpChannel(this, new Lazy<TcpChannel>(() => Connection, true));
    }

    private protected override void CloseConnection()
    {
        base.CloseConnection();
        if (Connection.Connected)
            Connection.Close();
        Connection = null;
    }
}