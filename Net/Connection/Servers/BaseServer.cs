namespace Net.Connection.Servers;

using Clients;
using Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public abstract class BaseServer<TClient, TChannel> where TClient : BaseClient<TChannel> where TChannel : Channels.IChannel
{
    protected virtual List<TClient> Clients { get; init; }
    protected virtual NetSettings Settings { get; init; }

    public abstract void Start();
    public virtual Task StartAsync() => Task.Run(Start);

    public abstract void SendMessageToAll(MessageBase msg);
    public abstract Task SendMessageToAllAsync(MessageBase msg);

    public virtual void ShutDown()
    {
        throw new NotImplementedException();
    }

    public virtual Task ShutDownAsync()
    {
        throw new NotImplementedException();
    }

    public virtual void Stop()
    {
        throw new NotImplementedException();
    }

    public virtual Task StopAsync()
    {
        throw new NotImplementedException();
    }
}