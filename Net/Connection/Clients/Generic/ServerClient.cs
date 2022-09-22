namespace Net.Connection.Clients.Generic;

using Channels;
using Messages;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// The out-of-the-box ServerClient is similar to the Client class, but it is designed to work on the server-side.
/// </summary>
public abstract class ServerClient<MainConnection> : ObjectClient<MainConnection> where MainConnection : class, IChannel
{
    protected IAsyncEnumerator<MessageBase> _reciever;

    internal async Task GetNextMessageAsync()
    {
        var msg = _reciever.Current;

        if (msg != null)
            HandleMessage(msg);

        await _reciever.MoveNextAsync();
    }
}

public class ServerClient : ServerClient<IChannel>
{
    public ServerClient(IChannel connection, NetSettings settings = null)
    {
        ConnectionState = ConnectState.PENDING;

        Settings = settings ?? new NetSettings();
        Connection = connection;

        _reciever = ReceiveMessagesAsync().GetAsyncEnumerator();

        (this as GeneralClient<IChannel>).SendMessage(new SettingsMessage(Settings));
    }
}