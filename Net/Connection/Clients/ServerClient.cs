namespace Net.Connection.Clients;

using Net.Messages;
using System.Collections.Generic;
using System.Net.Sockets;

public class ServerClient : ObjectClient
{
    private IEnumerator<MessageBase> _reciever;

    internal ServerClient(Socket soc, NetSettings settings = null) 
    {
        ConnectionState = ConnectState.PENDING;

        Settings = settings ?? new NetSettings();
        Soc = soc;

        _reciever = RecieveMessages().GetEnumerator();

        (this as GeneralClient<Channels.UdpChannel>).SendMessage(new SettingsMessage(Settings));
    }

    internal void GetNextMessage()
    {
        var msg = _reciever.Current;

        if (msg != null) 
            HandleMessage(msg);
        else if (!AwaitingPoll)
            StartConnectionPoll();
        
        _reciever.MoveNext();
    }
}