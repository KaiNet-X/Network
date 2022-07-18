namespace Net.Connection.Clients;

using Messages;
using Net.Connection.Channels;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public abstract class BaseClient 
{
    public volatile List<IChannel> Channels = new();

    public abstract void SendMessage(MessageBase message);

    public abstract Task SendMessageAsync(MessageBase message, CancellationToken token = default);

    protected abstract IEnumerable<MessageBase> ReceiveMessages();

    protected abstract IAsyncEnumerable<MessageBase> ReceiveMessagesAsync();

    public abstract IChannel OpenChannel();

    public abstract void CloseChannel(IChannel c);

    public abstract Task CloseChannelAsync(IChannel c, CancellationToken token = default);

    public abstract Task<IChannel> OpenChannelAsync(CancellationToken token = default);

    public abstract void Close();

    public abstract Task CloseAsync();
}