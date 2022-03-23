namespace Net.Connection.Servers
{
    using Clients;
    using Messages;
    using System.Collections.Generic;

    public abstract class ServerBase<TClient> where TClient : ClientBase
    {
        protected virtual List<TClient> Clients { get; init; }
        protected virtual NetSettings Settings { get; init; }
        public abstract void StartServer();

        public abstract void SendMessageToAll(MessageBase msg);

        public virtual void ShutDown()
        {
            
        }
    }
}
