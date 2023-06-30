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
    public int ConnectionPollTimeout { get; init; } = 8000;
}