namespace Net.Connection.Servers;

using Channels;
using Clients.NewTcp;
using Servers.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class TcpServer : Server<ServerClient, TcpChannel>
{
    private List<Socket> _bindingSockets;

    /// <summary>
    /// Endpoints passed to the server as arguments
    /// </summary>
    public readonly List<IPEndPoint> Endpoints;

    /// <summary>
    /// Endpoints of all active binding sockets
    /// </summary>
    public List<IPEndPoint> ActiveEndpoints => _bindingSockets.Select(s => (IPEndPoint)s.LocalEndPoint).ToList();

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="address">IP address for the server to bind to</param>
    /// <param name="port">Port for the server to bind to</param>
    /// <param name="maxClients">Max amount of clients</param>
    /// <param name="settings">Settings for connection</param>
    public TcpServer(IPAddress address, int port, ServerSettings settings = null) :
        this(new IPEndPoint(address, port), settings)
    { }

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="endpoint">Endpoint for the server to bind to</param>
    /// <param name="maxClients">Max amount of clients</param>
    /// <param name="settings">Settings for connection</param>
    public TcpServer(IPEndPoint endpoint, ServerSettings settings = null) :
        this(new List<IPEndPoint> { endpoint }, settings)
    { }

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="endpoints">List of endpoints for the server to bind to</param>
    /// <param name="maxClients">Max amount of clients</param>
    /// <param name="settings">Settings for connection</param>
    public TcpServer(List<IPEndPoint> endpoints, ServerSettings settings = null)
    {
        Settings = settings ?? new ServerSettings();
        Endpoints = endpoints;
        _bindingSockets = new List<Socket>();

        InitializeSockets(Endpoints);
    }

    public override void Stop()
    {
        Listening = false;
        Utilities.ConcurrentAccess(() =>
        {
            while (_bindingSockets.Count > 0)
            {
                _bindingSockets[0].Close();
                _bindingSockets.RemoveAt(0);
            }
        }, _semaphore);
    }

    public override async Task StopAsync()
    {
        Listening = false;

        await Utilities.ConcurrentAccessAsync((ct) =>
        {
            while (_bindingSockets.Count > 0)
            {
                _bindingSockets[0].Close();
                _bindingSockets.RemoveAt(0);
            }
            return ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;
        }, _semaphore);
    }

    protected override async Task<ServerClient> InitializeClient() =>
        new ServerClient(await GetNextConnection(), Settings);

    private void InitializeSockets(List<IPEndPoint> endpoints)
    {
        foreach (IPEndPoint endpoint in endpoints)
        {
            Socket s = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            s.Bind(endpoint);
            _bindingSockets.Add(s);
        }
    }

    private async Task<Socket> GetNextConnection()
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        List<Task<Socket>> tasks = new List<Task<Socket>>();

        foreach (Socket s in _bindingSockets)
        {
            tasks.Add(s.AcceptAsync(cts.Token).AsTask());
        }
        var connection = await await Task.WhenAny(tasks);
        cts.Cancel();

        return connection;
    }
}
