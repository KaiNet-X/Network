namespace Net;

public class ClientSettings
{
    /// <summary>
    /// Encrypt the main connection
    /// </summary>
    public required bool UseEncryption;

    /// <summary>
    /// Timeout for connection checks
    /// </summary>
    public required int ConnectionPollTimeout;

    /// <summary>
    /// Requires the client to register allowed types to deserialize
    /// </summary>
    public required bool RequiresRegisteredTypes;
}
