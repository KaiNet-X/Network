namespace Net.Connection.Clients;

using Messages;
using Net.Connection.Channels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public abstract class BaseClient<TChannel> where TChannel : IChannel
{
    public volatile Dictionary<Guid, TChannel> Channels = new();

    public abstract void SendMessage(MessageBase message);

    public abstract Task SendMessageAsync(MessageBase message, CancellationToken token = default);

    protected abstract IEnumerable<MessageBase> RecieveMessages();

    public abstract TChannel OpenChannel();

    public abstract void CloseChannel(TChannel c);

    public abstract Task CloseChannelAsync(TChannel c, CancellationToken token = default);

    public abstract Task<TChannel> OpenChannelAsync(CancellationToken token = default);

    public abstract void Close();

    public abstract Task CloseAsync();
}