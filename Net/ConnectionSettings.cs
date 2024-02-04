namespace Net;

public class ConnectionSettings
{
    /// <summary>
    /// Encrypt the main connection
    /// </summary>
    public bool UseEncryption { get; init; } = true;

    /// <summary>
    /// Timeout for connection checks
    /// </summary>
    public int ConnectionPollTimeout { get; init; } = 1000;

    /// <summary>
    /// Requires the client to register allowed types to deserialize
    /// </summary>
    public bool RequiresWhitelistedTypes = false;
}
