namespace Net.Connection.Clients;

using Messages;
using System.Collections.Generic;
using System.Threading.Tasks;

public abstract class ClientBase
{
    protected volatile NetSettings Settings;

    protected volatile bool Initialized = false;
    protected volatile bool Waiting = false;

    public abstract void SendMessage(MessageBase message);
    public virtual Task SendMessageAsync(MessageBase message) => Task.Run(() => SendMessage(message));

    protected abstract IEnumerable<MessageBase> RecieveMessages();
}
