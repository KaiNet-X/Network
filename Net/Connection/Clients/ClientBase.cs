using Net.Messages;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Net
{
    public abstract class ClientBase
    {
        protected NetSettings Settings;
        protected List<byte[]> SendQueue = new List<byte[]>();

        protected bool Initialized = false;
        protected bool Waiting = false;

        protected internal virtual IEnumerator Reciever { get; set; }

        public abstract void Connect();
        public virtual Task ConnectAsync() => Task.Run(() => Connect());

        public abstract void SendMessage(MessageBase message);
        public virtual Task SendMessageAsync(MessageBase message) => Task.Run(() => SendMessage(message));

        protected internal abstract IEnumerator Recieve();
    }
}
