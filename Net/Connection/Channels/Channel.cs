using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Net.Connection.Channels
{
    public class Channel : IChannel
    {
        private bool _connected = false;
        public bool Connected 
        {
            get => _connected;
            set
            {
                _connected = value;
                if (value)
                { 
                    while (_sendBytes.Count > 0)
                    {
                        SendBytes(_sendBytes[0]);
                        _sendBytes.RemoveAt(0);
                    }
                }
            }
        }

        public readonly Guid Id;
        public int Port => (Udp.Client.LocalEndPoint as IPEndPoint).Port;
        private IPEndPoint remoteEndpoint;
        private bool disposedValue;
        private readonly UdpClient Udp;
        private List<byte[]> _sendBytes = new List<byte[]>();

        public Channel(IPAddress localAddr, IPEndPoint remote, Guid? id = null)
        {
            this.Id = id??Guid.NewGuid();
            Udp = new UdpClient(new IPEndPoint(localAddr, 0));
            Udp.Connect(remote);
            remoteEndpoint = remote;
        }

        public void SendBytes(byte[] data)
        {
            if (!Connected)
            {
                _sendBytes.Add(data);
                return;
            }
            Udp.Send(data, data.Length);
            //Udp.Dispose();
        }

        public byte[] RecieveBytes() =>
            Udp.Receive(ref remoteEndpoint);

        public async Task SendBytesAsync(byte[] data)
        {
            while (!Connected) ;
            await Udp.SendAsync(data, data.Length);
            Udp.Dispose();
        }

        public async Task<byte[]> RecieveBytesAsync() =>
            (await Udp.ReceiveAsync()).Buffer;

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
