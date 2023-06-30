namespace Net.Connection.Clients.Tcp;

using Channels;
using Messages;
using Net.Connection.Clients.Generic;
using Net.Connection.Servers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

/// <summary>
/// The out-of-the-box ServerClient is similar to the Client class, but it is designed to work on the server-side.
/// </summary>
public class ServerClient : ObjectClient
{
    private IAsyncEnumerator<MessageBase> _receiver;
    protected internal Task connectedTask => Connected.Task;

    /// <summary>
    /// If the controll loop fails, get the exception
    /// </summary>
    public Exception ControlLoopException { get; private set; }

    internal ServerClient(Socket soc, ServerSettings settings = null) : base()
    {
        ConnectionState = ConnectState.PENDING;

        Settings = settings ?? new ServerSettings();
        Connection = new TcpChannel(soc);

        localEndPoint = Connection.Socket.LocalEndPoint as IPEndPoint;
        remoteEndPoint = Connection.Socket.RemoteEndPoint as IPEndPoint;

        _receiver = ReceiveMessagesAsync().GetAsyncEnumerator();

        (this as GeneralClient<TcpChannel>).SendMessage(new SettingsMessage(Settings));
    }

    internal async Task GetNextMessageAsync()
    {
        try
        {
            var msg = _receiver.Current;

            if (msg != null)
                await HandleMessageAsync(msg);

            await _receiver.MoveNextAsync();
        }
        catch (Exception ex)
        {
            ControlLoopException = ex;
            throw;
        }
    }
}