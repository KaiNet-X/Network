using Net.Messages;
using System.Collections.Generic;

namespace Net
{
    public abstract class ServerBase<TClient> where TClient : ClientBase
    {
        internal virtual List<TClient> Clients { get; init; }
        internal virtual NetSettings Settings { get; init; }
        public abstract void StartServer();

        public virtual void SendMessageToAll(MessageBase msg)
        {
            lock (Clients)
                foreach (TClient c in Clients) c.SendMessage(msg);
        }

        public virtual void ShutDown()
        {

        }
    }
}
