namespace Net.Connection.Clients;

using Messages;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public abstract class ClientBase
{
    protected volatile NetSettings Settings;

    public abstract void SendMessage(MessageBase message);
    public abstract Task SendMessageAsync(MessageBase message, CancellationToken token = default);

    protected abstract IEnumerable<MessageBase> RecieveMessages();
}
