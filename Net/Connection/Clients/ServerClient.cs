namespace Net.Connection.Clients;

using Net.Messages;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

public class ServerClient : ObjectClient
{
    private IAsyncEnumerator<MessageBase> _reciever;

    internal ServerClient(Socket soc, NetSettings settings = null) 
    {
        ConnectionState = ConnectState.PENDING;

        Settings = settings ?? new NetSettings();
        Soc = soc;

        _reciever = ReceiveMessagesAsync().GetAsyncEnumerator();

        (this as GeneralClient).SendMessage(new SettingsMessage(Settings));
    }

    internal async Task GetNextMessageAsync()
    {
        var msg = _reciever.Current;

        if (msg != null) 
            HandleMessage(msg);
        else if (!AwaitingPoll)
            StartConnectionPoll();
        
        await _reciever.MoveNextAsync();
    }
}