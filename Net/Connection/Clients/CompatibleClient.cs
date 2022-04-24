using Net.Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Connection.Clients
{
    public class CompatibleClient : GeneralClient
    {
        public new void SendMessage(MessageBase message)
        {
            if (ConnectionState == ConnectState.CONNECTED)
                base.SendMessage(message);
        }

        public new async Task SendMessageAsync(MessageBase message, CancellationToken token = default)
        {
            if (ConnectionState == ConnectState.CONNECTED)
                await base.SendMessageAsync(message, token);
        }
    }
}