namespace Net.Connection.Clients.Tcp;

using Channels;
using Generic;
using Messages;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using Net;

public class ServerClient : ObjectClient, IServerClient
{
    Task IServerClient.connectedTask => Connected.Task;
    private IAsyncEnumerator<MessageBase> _receiver;

    /// <summary>
    /// If the control loop fails, get the exception
    /// </summary>
    public Exception ControlLoopException { get; protected set; }

    internal ServerClient(Socket soc, ConnectionSettings settings) : base()
    {
        ConnectionState = ConnectionState.PENDING;

        Settings = settings;
        Connection = new TcpChannel(soc);

        LocalEndpoint = Connection.Socket.LocalEndPoint as IPEndPoint;
        RemoteEndpoint = Connection.Socket.RemoteEndPoint as IPEndPoint;

        _receiver = ReceiveMessagesAsync().GetAsyncEnumerator();
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

    void IServerClient.SetRegisteredObjectTypes(HashSet<Type> registeredTypes) =>
        WhitelistedObjectTypes = registeredTypes;
}
