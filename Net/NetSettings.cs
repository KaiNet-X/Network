namespace Net;

public class NetSettings
{
    public bool UseEncryption { get; set; } = true;
    public bool EncryptChannels { get; set; } = true;
    public bool SingleThreadedServer { get; set; } = false;
    public int ConnectionPollTimeout { get; set; } = 8000;
}