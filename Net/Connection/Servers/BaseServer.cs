namespace Net.Connection.Servers;

using Clients;
using Messages;
using Channels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public abstract class BaseServer<TClient, TChannel> where TClient : BaseClient<TChannel> where TChannel : IChannel
{
    public virtual List<TClient> Clients { get; init; }

    public abstract void Start();
    public abstract Task StartAsync();

    public abstract void SendMessageToAll(MessageBase msg);
    public abstract Task SendMessageToAllAsync(MessageBase msg, CancellationToken token = default);

    public abstract void ShutDown();

    public abstract Task ShutDownAsync();

    public abstract void Stop();

    public abstract Task StopAsync();
}