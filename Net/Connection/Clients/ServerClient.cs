namespace Net.Connection.Clients;

using Net.Messages;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;

public class ServerClient : ObjectClient
{
    private IEnumerator<MessageBase> _reciever;
    private Stopwatch _timer = new Stopwatch();

    internal ServerClient(Socket soc, NetSettings settings = null) 
    {
        ConnectionState = ConnectState.PENDING;

        Settings = settings ?? new NetSettings();
        Soc = soc;

        _reciever = RecieveMessages().GetEnumerator();

        (this as GeneralClient<Channels.Channel>).SendMessage(new SettingsMessage(Settings));
    }

    internal async Task GetNextMessage()
    {
        var msg = _reciever.Current;
        if (msg != null) 
            await HandleMessage(msg);
        else if (ConnectionState == ConnectState.CONNECTED)
        {
            if (_timer == null) _timer = Stopwatch.StartNew();
            else if (_timer?.ElapsedMilliseconds == 0)
                _timer.Restart();
            else if (_timer.ElapsedMilliseconds >= 1000)
            {
                _timer.Reset();
                StartConnectionPoll();
            }
        }
        _reciever.MoveNext();
    }
}