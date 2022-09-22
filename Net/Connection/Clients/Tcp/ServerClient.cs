﻿namespace Net.Connection.Clients.Tcp;

using Channels;
using Messages;
using Net.Connection.Clients.Generic;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

/// <summary>
/// The out-of-the-box ServerClient is similar to the Client class, but it is designed to work on the server-side.
/// </summary>
public class ServerClient : ObjectClient
{
    private IAsyncEnumerator<MessageBase> _reciever;

    internal ServerClient(Socket soc, NetSettings settings = null) : base()
    {
        ConnectionState = ConnectState.PENDING;

        Settings = settings ?? new NetSettings();
        Connection = new TcpChannel(soc);

        _reciever = ReceiveMessagesAsync().GetAsyncEnumerator();

        (this as GeneralClient<TcpChannel>).SendMessage(new SettingsMessage(Settings));
    }

    internal async Task GetNextMessageAsync()
    {
        var msg = _reciever.Current;

        if (msg != null)
            HandleMessage(msg);

        await _reciever.MoveNextAsync();
    }
}