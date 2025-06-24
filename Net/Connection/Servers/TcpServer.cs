namespace Net.Connection.Servers;

using Channels;
using Clients.Tcp;
using Net;
using Net.Connection.Clients.Generic;
using Net.Messages;
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
    /// <param name="settings">Settings for connection</param>
    public TcpServer(IPAddress address, int port, ServerSettings settings = null) :
        this(new IPEndPoint(address, port), settings)
    { }

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="endpoint">Endpoint for the server to bind to</param>
    /// <param name="settings">Settings for connection</param>
    public TcpServer(IPEndPoint endpoint, ServerSettings settings = null) :
        this([endpoint], settings)
    { }

    /// <summary>
    /// New server object
    /// </summary>
    /// <param name="endpoints">List of endpoints for the server to bind to</param>
    /// <param name="settings">Settings for connection</param>
    public TcpServer(List<IPEndPoint> endpoints, ServerSettings settings = null) : base(settings ?? new ServerSettings())
    {
        Endpoints = endpoints;
        _bindingSockets = [];
    }

    /// <inheritdoc/>
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
    
    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override void Start()
    {
        InitializeSockets(Endpoints);
        StartListening();
        base.Start();
    }

    /// <inheritdoc/>
    protected override async Task<ServerClient> InitializeClient()
    {
        Socket connection;
        using (var cts = new CancellationTokenSource())
        {
            var tasks = _bindingSockets.Select(s => s.AcceptAsync(cts.Token).AsTask());

            connection = await await Task.WhenAny(tasks);
            await cts.CancelAsync();
        }

        var settings = new ConnectionSettings
        {
            UseEncryption = Settings.UseEncryption,
            ConnectionPollTimeout = Settings.ConnectionPollTimeout,
            RequireWhitelistedTypes = Settings.RequireWhitelistedTypes,
        };
        
        var sc = new ServerClient(connection, settings);

        await sc.SendMessageAsync(new SettingsMessage(settings));

        return sc;
    }

    private void InitializeSockets(List<IPEndPoint> endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            var s = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            s.Bind(endpoint);
            _bindingSockets.Add(s);
        }
    }

    private void StartListening()
    {
        foreach (var s in _bindingSockets)
            s.Listen();
    }
}
