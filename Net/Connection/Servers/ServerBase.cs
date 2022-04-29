namespace Net.Connection.Servers;

using Clients;
using Messages;
using System.Collections.Generic;
using System.Threading.Tasks;

public abstract class ServerBase<TClient, TChannel> where TClient : ClientBase<TChannel> where TChannel : Channels.IChannel
{
    protected virtual List<TClient> Clients { get; init; }
    protected virtual NetSettings Settings { get; init; }

    public abstract void StartServer();
    public virtual Task StartServerAsync() => Task.Run(StartServer);

    public abstract void SendMessageToAll(MessageBase msg);
    public abstract Task SendMessageToAllAsync(MessageBase msg);

    public virtual void ShutDown()
    {
        throw new System.NotImplementedException();
    }

    public virtual Task ShutDownAsync()
    {
        throw new System.NotImplementedException();
    }
}