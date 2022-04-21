namespace Net;

using System.IO;

public class NetSettings
{
    public bool UseEncryption = true;
    public bool SingleThreadedServer = true;
    public int ConnectionPollTimeout = 8000;
    public string FilePath = Directory.GetCurrentDirectory() + @"\Recieved";
}