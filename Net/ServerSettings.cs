namespace Net.Connection.Servers;

/// <summary>
/// Settings that you can pass to the server
/// </summary>
public class ServerSettings
{
    /// <summary>
    /// Encrypt the main connection
    /// </summary>
    public bool UseEncryption { get; init; } = true;

    /// <summary>
    /// Run serverclients on one thread or dedicated threads
    /// </summary>
    public bool SingleThreadedServer { get; init; } = false;

    /// <summary>
    /// Timeout for connection checks
    /// </summary>
    public int ConnectionPollTimeout { get; init; } = 1000;

    /// <summary>
    /// Remove clients from the list after disconnection is invoked
    /// </summary>
    public bool RemoveClientAfterDisconnect { get; init; } = true;

    /// <summary>
    /// Max connections at one time. If less than one, considered to be unset
    /// </summary>
    public int MaxClientConnections { get; init; } = -1;

    /// <summary>
    /// Requires the server and client programs to 
    /// </summary>
    public bool ServerRequiresWhitelistedTypes { get; set; } = true;

    /// <summary>
    /// Requires the server and client programs to 
    /// </summary>
    public bool ClientRequiresWhitelistedTypes { get; set; } = false;
}
