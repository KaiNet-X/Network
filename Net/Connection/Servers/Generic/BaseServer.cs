namespace Net.Connection.Servers.Generic;

using Messages;
using Net.Connection.Clients.Generic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Base class for all servers
/// </summary>
/// <typeparam name="TClient">Generic client type. This must inherrit base client, and is used to keep a consistent client implementation on the server.</typeparam>
public abstract class BaseServer<TClient> where TClient : BaseClient
{
    public virtual List<TClient> Clients { get; init; }

    /// <summary>
    /// Starts listening for incoming connections
    /// </summary>
    public abstract void Start();

    /// <summary>
    /// Starts listening for incoming connections
    /// </summary>
    public abstract Task StartAsync();

    /// <summary>
    /// Sends a message to all clients
    /// </summary>
    /// <param name="msg"></param>
    public abstract void SendMessageToAll(MessageBase msg);

    /// <summary>
    /// Sends a message to all clients
    /// </summary>
    /// <param name="msg"></param>
    public abstract Task SendMessageToAllAsync(MessageBase msg, CancellationToken token = default);

    /// <summary>
    /// Completely shuts the server down and closes all connections
    /// </summary>
    public abstract void ShutDown();

    /// <summary>
    /// Completely shuts the server down and closes all connections
    /// </summary>
    public abstract Task ShutDownAsync();

    /// <summary>
    /// Stops listening for new client connections
    /// </summary>
    public abstract void Stop();

    /// <summary>
    /// Stops listening for new client connections
    /// </summary>
    public abstract Task StopAsync();
}