namespace Net.Connection.Clients;

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class Client : GeneralClient
{
    public readonly IPAddress Address;
    public readonly uint Port;

    private Stopwatch _timer = new Stopwatch();

    public Client(IPAddress address, uint port)
    {
        this.ConnectionState = ConnectState.PENDING;
        this.Address = address;
        InitializeSocket();

        this.Address = address;
        this.Port = port;
    }

    public void Connect(int maxAttempts = 0, bool throwWhenExausted = false) =>
        Task.Run(async () => await ConnectAsync(maxAttempts, throwWhenExausted)).GetAwaiter().GetResult();

    public async Task ConnectAsync(int maxAttempts = 0, bool throwWhenExausted = false)
    {
        if (Soc == null) InitializeSocket();

        Task.Run(async () =>
        {
            try
            {
                foreach (var msg in RecieveMessages())
                    if (msg != null)
                        await HandleMessage(msg);
                    else
                    {
                        if (_timer == null) _timer = Stopwatch.StartNew();
                        else if (_timer?.ElapsedMilliseconds == 0)
                            _timer.Restart();
                        else if (_timer.ElapsedMilliseconds >= 1000)
                        {
                            _timer.Reset();
                            StartConnectionPoll();
                        }
                    }
            }
            catch (Exception ex)
            {

            }
        });

        for (int i = 0; i <= maxAttempts; i++)
        {
            try
            {
                await Soc.ConnectAsync(new IPEndPoint(Address, (int)Port));
                break;
            }
            catch
            {
                if (i == maxAttempts && throwWhenExausted) 
                    throw;
            }
        }
        ConnectionState = ConnectState.CONNECTED;
    }

    private void InitializeSocket()
    {
        Soc = Address.AddressFamily == AddressFamily.InterNetwork ?
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) :
            new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
    }
}