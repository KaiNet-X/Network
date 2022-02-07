namespace Net.Connection.Clients
{
    using Messages;
    using System.Collections;
    using System.Threading.Tasks;

    public abstract class ClientBase
    {
        protected volatile NetSettings Settings;
        //protected List<byte[]> SendQueue = new List<byte[]>();

        protected volatile bool Initialized = false;
        protected volatile bool Waiting = false;

        protected volatile internal IEnumerator Reciever;

        public abstract void SendMessage(MessageBase message);
        public virtual Task SendMessageAsync(MessageBase message) => Task.Run(() => SendMessage(message));

        protected internal abstract IEnumerator Recieve();
    }
}
