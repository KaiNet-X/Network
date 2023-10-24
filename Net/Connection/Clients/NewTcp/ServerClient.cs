namespace Net.Connection.Clients.NewTcp;

using Channels;
using Servers;
using Generic;
using Messages;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ServerClient : ObjectClient, IServerClient
{
    Task IServerClient.connectedTask => Connected.Task;
    private IAsyncEnumerator<MessageBase> _receiver;

    /// <summary>
    /// If the control loop fails, get the exception
    /// </summary>
    public Exception ControlLoopException { get; protected set; }

    internal ServerClient(Socket soc, ServerSettings settings = null) : base()
    {
        ConnectionState = ConnectionState.PENDING;

        Settings = settings ?? new ServerSettings();
        Connection = new TcpChannel(soc);

        LocalEndpoint = Connection.Socket.LocalEndPoint as IPEndPoint;
        RemoteEndpoint = Connection.Socket.RemoteEndPoint as IPEndPoint;

        _receiver = ReceiveMessagesAsync().GetAsyncEnumerator();

        (this as GeneralClient<TcpChannel>).SendMessage(new SettingsMessage(Settings));
    }

    async Task IServerClient.ReceiveNextAsync()
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
