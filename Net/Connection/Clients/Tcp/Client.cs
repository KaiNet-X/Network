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
    /// <param name="throwWhenExhausted">Throw exception if connection didn't work</param>
    /// <returns>true if connected, otherwise false</returns>
    public bool Connect(IPEndPoint serverEndPoint, ulong maxAttempts = 0, bool throwWhenExhausted = false)
    {
        if (ConnectionState is ConnectionState.PENDING or ConnectionState.CONNECTED)
            throw new InvalidOperationException("Tried to connect when there is already a connection in progress.");

        var exceptions = throwWhenExhausted ? new List<Exception>() : null;

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
                exceptions?.Add(e);

                if (i != maxAttempts) continue;
                
                return Return();
            }
        }

        LocalEndpoint = Connection.Socket.LocalEndPoint as IPEndPoint;
        RemoteEndpoint = Connection.Socket.RemoteEndPoint as IPEndPoint;

        while (ConnectionState == ConnectionState.PENDING) Thread.Sleep(10);

        return true;
        
        bool Return()
        {
            ConnectionState = ConnectionState.NONE;
            
            if (throwWhenExhausted && exceptions?.Count > 0)
                throw new AggregateException(exceptions);
            
            return false;
        }
    }

    public bool Connect(IPAddress serverAddress, int serverPort, ulong maxAttempts = 0, bool throwWhenExhausted = false) =>
        Connect(new IPEndPoint(serverAddress, serverPort), maxAttempts, throwWhenExhausted);

    public bool Connect(string serverAddress, int serverPort, ulong maxAttempts = 0, bool throwWhenExhausted = false) =>
        Connect(IPAddress.Parse(serverAddress), serverPort, maxAttempts, throwWhenExhausted);

    /// <summary>
    /// Connect to the server this client is bound to
    /// </summary>
    /// <param name="serverEndPoint"></param>
    /// <param name="maxAttempts">Max amount of connection attempts</param>
    /// <param name="throwWhenExhausted">Throw exception if connection didn't work</param>
    /// <param name="cancellationToken"></param>
    /// <returns>true if connected, otherwise false</returns>
    public async Task<bool> ConnectAsync(IPEndPoint serverEndPoint, ulong maxAttempts = 0, bool throwWhenExhausted = false, CancellationToken? cancellationToken = null)
    {
        if (ConnectionState is ConnectionState.PENDING or ConnectionState.CONNECTED)
            throw new InvalidOperationException("Tried to connect when there is already a connection in progress.");

        var soc = new Socket(serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        ConnectionState = ConnectionState.PENDING;

        var exceptions = throwWhenExhausted ? new List<Exception>() : null;

        cancellationToken?.Register(() =>
        {
            soc.Close();
        });

        for (ulong i = 0; i <= maxAttempts; i++)
        {
            if (maxAttempts == 0)
                i--;
            
            if (cancellationToken?.IsCancellationRequested ?? false)
                return Return();
            
            try
            {
                await soc.ConnectAsync(serverEndPoint);
                Connection = new TcpChannel(soc);
                StartLoop(DisconnectTokenSource.Token);
                break;
            }
            catch (Exception e)
            {
                exceptions?.Add(e);

                if (i != maxAttempts) continue;

                return Return();
            }
        }

        LocalEndpoint = Connection.Socket.LocalEndPoint as IPEndPoint;
        RemoteEndpoint = Connection.Socket.RemoteEndPoint as IPEndPoint;

        await ConnectedTask.Task;

        return true;

        bool Return()
        {
            ConnectionState = ConnectionState.NONE;
            
            if (throwWhenExhausted && exceptions?.Count > 0)
                throw new AggregateException(exceptions);
            
            return false;
        }
    }

    public Task<bool> ConnectAsync(IPAddress serverAddress, int serverPort, ulong maxAttempts = 0, bool throwWhenExhausted = false, CancellationToken? cancellationToken = null) =>
        ConnectAsync(new IPEndPoint(serverAddress, serverPort), maxAttempts, throwWhenExhausted, cancellationToken);

    public Task<bool> ConnectAsync(string serverAddress, int serverPort, ulong maxAttempts = 0, bool throwWhenExhausted = false, CancellationToken? cancellationToken = null) =>
        ConnectAsync(IPAddress.Parse(serverAddress), serverPort, maxAttempts, throwWhenExhausted, cancellationToken);

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