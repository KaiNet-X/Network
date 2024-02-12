namespace Net.Connection.Clients.Tcp;

using Channels;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// The out-of-the-box Client implementation allows sending objects to the server, managing UDP channels, and follows an event based approach to receiving data.
/// </summary>
public class Client : ObjectClient
{
    private Task _listener { get; set; }

    /// <summary>
    /// If the control loop fails, get the exception
    /// </summary>
    public Exception ControlLoopException => _listener.Exception;

    /// <summary>
    /// Connect to the server this client is bound to
    /// </summary>
    /// <param name="serverEndPoint"></param>
    /// <param name="maxAttempts">Max amount of connection attempts</param>
    /// <param name="throwWhenExausted">Throw exception if connection didn't work</param>
    /// <returns>true if connected, otherwise false</returns>
    public bool Connect(IPEndPoint serverEndPoint, ulong maxAttempts = 0, bool throwWhenExausted = false)
    {
        if (ConnectionState == ConnectionState.PENDING || ConnectionState == ConnectionState.CONNECTED)
            throw new InvalidOperationException("Tried to connect when there is already a connection in progress.");

        List<Exception> exceptions = null;

        var soc = new Socket(serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        ConnectionState = ConnectionState.PENDING;

        for (ulong i = 0; i <= maxAttempts; i++)
        {
            if (maxAttempts == 0) 
                i--;
            try
            {
                soc.Connect(serverEndPoint);
                Connection = new TcpChannel(soc);
                StartLoop(DisconnectTokenSource.Token);
                break;
            }
            catch (Exception e)
            {
                if (throwWhenExausted && exceptions == null)
                    exceptions = new List<Exception>();

                exceptions?.Add(e);

                if (i == maxAttempts)
                    if (throwWhenExausted)
                        throw new AggregateException(exceptions);
                    else
                        return false;
            }
        }

        LocalEndpoint = Connection.Socket.LocalEndPoint as IPEndPoint;
        RemoteEndpoint = Connection.Socket.RemoteEndPoint as IPEndPoint;

        while (ConnectionState == ConnectionState.PENDING) Thread.Sleep(10);

        return true;
    }

    public bool Connect(IPAddress serverAddress, int serverPort, ulong maxAttempts = 0, bool throwWhenExausted = false) =>
        Connect(new IPEndPoint(serverAddress, serverPort), maxAttempts, throwWhenExausted);

    public bool Connect(string serverAddress, int serverPort, ulong maxAttempts = 0, bool throwWhenExausted = false) =>
        Connect(IPAddress.Parse(serverAddress), serverPort, maxAttempts, throwWhenExausted);

    /// <summary>
    /// Connect to the server this client is bound to
    /// </summary>
    /// <param name="serverEndPoint"></param>
    /// <param name="maxAttempts">Max amount of connection attempts</param>
    /// <param name="throwWhenExausted">Throw exception if connection didn't work</param>
    /// <returns>true if connected, otherwise false</returns>
    public async Task<bool> ConnectAsync(IPEndPoint serverEndPoint, ulong maxAttempts = 0, bool throwWhenExausted = false)
    {
        if (ConnectionState == ConnectionState.PENDING || ConnectionState == ConnectionState.CONNECTED)
            throw new InvalidOperationException("Tried to connect when there is already a connection in progress.");

        var soc = new Socket(serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        ConnectionState = ConnectionState.PENDING;

        List<Exception> exceptions = null;

        for (ulong i = 0; i <= maxAttempts; i++)
        {
            if (maxAttempts == 0)
                i--;
            try
            {
                await soc.ConnectAsync(serverEndPoint);
                Connection = new TcpChannel(soc);
                StartLoop(DisconnectTokenSource.Token);
                break;
            }
            catch (Exception e)
            {
                if (throwWhenExausted && exceptions == null)
                    exceptions = new List<Exception>();

                exceptions?.Add(e);

                if (i == maxAttempts)
                    if (throwWhenExausted)
                        throw new AggregateException(exceptions);
                    else
                        return false;
            }
        }

        LocalEndpoint = Connection.Socket.LocalEndPoint as IPEndPoint;
        RemoteEndpoint = Connection.Socket.RemoteEndPoint as IPEndPoint;

        await ConnectedTask.Task;

        return true;
    }

    public Task<bool> ConnectAsync(IPAddress serverAddress, int serverPort, ulong maxAttempts = 0, bool throwWhenExausted = false) =>
        ConnectAsync(new IPEndPoint(serverAddress, serverPort), maxAttempts, throwWhenExausted);

    public Task<bool> ConnectAsync(string serverAddress, int serverPort, ulong maxAttempts = 0, bool throwWhenExausted = false) =>
        ConnectAsync(IPAddress.Parse(serverAddress), serverPort, maxAttempts, throwWhenExausted);

    public void WhitelistObjectType(Type type) =>
        WhitelistedObjectTypes.Add(type);

    public void WhitelistObjectType<T>() =>
        WhitelistedObjectTypes.Add(typeof(T));

    private void StartLoop(CancellationToken ct)
    {
        _listener = Task.Factory.StartNew(async () =>
        {
            await foreach (var msg in ReceiveMessagesAsync())
                if (msg != null && !ct.IsCancellationRequested)
                    await HandleMessageAsync(msg);

        }, TaskCreationOptions.LongRunning);
    }
}