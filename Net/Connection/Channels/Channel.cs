using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Net.Connection.Channels
{
    public class Channel : IChannel
    {
        public bool Running;
        public readonly Guid Id;
        private IPEndPoint remoteEndpoint;
        private bool disposedValue;
        private readonly UdpClient Udp;

        public Channel(IPEndPoint local, IPEndPoint remote, Guid? id = null)
        {
            this.Id = id??Guid.NewGuid();
            Udp = new UdpClient(local);
            remoteEndpoint = remote;
        }

        public void SendBytes(byte[] data)
        {
            Running = true;
            Udp.Send(data, data.Length, remoteEndpoint);
            Udp.Dispose();
            Running = false;
        }

        public byte[] RecieveBytes()
        {
            return Udp.Receive(ref remoteEndpoint);
        }

        public async Task SendBytesAsync(byte[] data)
        {
            Running = true;
            await Udp.SendAsync(data, data.Length, remoteEndpoint);
            Udp.Dispose();
            Running = false;
        }

        public async Task<byte[]> RecieveBytesAsync()
        {
            var result = await Udp.ReceiveAsync();
            if (result.RemoteEndPoint.Address.Equals(remoteEndpoint.Address) && result.RemoteEndPoint.Port == remoteEndpoint.Port)
                return result.Buffer;
            return new byte[0];
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    Udp.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Channel()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        //public async Task<List<byte>> Listen()
        //{
        //    Running = true;
        //    List<byte> bytes = new List<byte>();
        //    while (Running)
        //    {
        //        UdpReceiveResult res = await Udp.ReceiveAsync();
        //        bytes.AddRange(res.Buffer);
        //    }
        //    return bytes;
        //}
    }
}
