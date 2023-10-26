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
    private readonly IPEndPoint _targetEndpoint;

    private Task _listener { get; set; }

    /// <summary>
    /// If the control loop fails, get the exception
    /// </summary>
    public Exception ControlLoopException => _listener.Exception;

    /// <summary>
    /// Initializes a new client
    /// </summary>
    /// <param name="address">IP address of server</param>
    /// <param name="port">Server port the client will connect to</param>
    public Client(IPAddress address, int port) : this(new IPEndPoint(address, port)) { }

    /// <summary>
    /// Initializes a new client
    /// </summary>
    /// <param name="address">IP address of server</param>
    /// <param name="port">Server port the client will connect to</param>
    public Client(string address, int port) : this(IPAddress.Parse(address), port) { }

    /// <summary>
    /// Initializes a new client
    /// </summary>
    /// <param name="ep">IPEndpoint of the server</param>
    public Client(IPEndPoint ep) : base()
    {
        ConnectionState = ConnectionState.PENDING;
        _targetEndpoint = ep;

        Initialize();
    }

    /// <summary>
    /// Connect to the server this client is bound to
    /// </summary>
    /// <param name="maxAttempts">Max amount of connection attempts</param>
    /// <param name="throwWhenExausted">Throw exception if connection didn't work</param>
    /// <returns>true if connected, otherwise false</returns>
    public bool Connect(ulong maxAttempts = 0, bool throwWhenExausted = false)
    {
        List<Exception> exceptions = null;

        if (Connection == null) Initialize();

        for (ulong i = 0; i <= maxAttempts; i++)
        {
            if (maxAttempts == 0) 
                i--;
            try
            {
                Connection.Socket.Connect(_targetEndpoint);
                StartLoop();
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

        while (ConnectionState == ConnectionState.PENDING) ;
        return true;
    }

    /// <summary>
    /// Connect to the server this client is bound to
    /// </summary>
    /// <param name="maxAttempts">Max amount of connection attempts</param>
    /// <param name="throwWhenExausted">Throw exception if connection didn't work</param>
    /// <returns>true if connected, otherwise false</returns>
    public async Task<bool> ConnectAsync(ulong maxAttempts = 0, bool throwWhenExausted = false)
    {
        List<Exception> exceptions = null;

        if (Connection == null) Initialize();

        for (ulong i = 0; i <= maxAttempts; i++)
        {
            if (maxAttempts == 0)
                i--;
            try
            {
                await Connection.Socket.ConnectAsync(_targetEndpoint);
                StartLoop();
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

        await Connected.Task;

        return true;
    }

    private void Initialize()
    {
        Connection = new TcpChannel(new Socket(_targetEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp));

        ConnectionState = ConnectionState.PENDING;
        TokenSource = new CancellationTokenSource();
    }

    private void StartLoop()
    {
        _listener = Task.Factory.StartNew(async () =>
        {
            await foreach (var msg in ReceiveMessagesAsync())
            {
                if (msg != null)
                    await HandleMessageAsync(msg);
            }
        }, TaskCreationOptions.LongRunning);
    }
}