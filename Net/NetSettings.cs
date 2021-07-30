using System.IO;

namespace Net
{
    public class NetSettings
    {
        public bool UseEncryption = true;
        public bool SingleThreadedServer = false;
        public string FilePath = Directory.GetCurrentDirectory() + @"\Recieved";
    }
}
