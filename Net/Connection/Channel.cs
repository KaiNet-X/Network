using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Net.Connection
{
    public class Channel
    {
        public bool Running;
        public readonly string ID;
        private readonly UdpClient Udp;
        public Channel(bool isSender, string ID, IPEndPoint endpoint)
        {
            this.ID = ID;
            Udp = new UdpClient(endpoint);
        }

        public async Task SendData(byte[] data)
        {
            Running = true;
            await Udp.SendAsync(data, data.Length);
            Udp.Dispose();
            Running = false;
        }

        public async Task<List<byte>> Listen()
        {
            Running = true;
            List<byte> bytes = new List<byte>();
            while (Running)
            {
                UdpReceiveResult res = await Udp.ReceiveAsync();
                bytes.AddRange(res.Buffer);
            }
            return bytes;
        }
    }
}
