namespace Net.Connection.Clients.Generic;

using Channels;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

/// <summary>
/// The out-of-the-box Client implementation allows sending objects to the server, managing UDP channels, and follows an event based approach to receiving data.
/// </summary>
public abstract class Client<MainConnection> : ObjectClient<MainConnection> where MainConnection : class, IChannel
{
    /// <summary>
    /// Delay between client updates; highly reduces CPU usage
    /// </summary>
    public ushort LoopDelay = 1;
    private readonly IPEndPoint _targetEndpoint;

    public Task _listener { get; private set; }

    /// <summary>
    /// Connect to the server this client is bound to
    /// </summary>
    /// <returns>true if connected, otherwise false</returns>
    public abstract bool Connect();

    /// <summary>
    /// Connect to the server this client is bound to
    /// </summary>
    /// <param name="maxAttempts">Max amount of connection attempts</param>
    /// <param name="throwWhenExausted">Throw exception if connection didn't work</param>
    /// <returns>true if connected, otherwise false</returns>
    public abstract Task<bool> ConnectAsync();

    private void StartLoop()
    {
        _listener = Task.Run(async () =>
        {
            await foreach (var msg in ReceiveMessagesAsync())
            {
                if (msg != null)
                    HandleMessage(msg);
            }
        });
    }
}

public class Client : Client<IChannel>
{
    public Func<bool> ConnectMethod;
    public Func<Task<bool>> ConnectMethodAsync;

    private Task _listener { get; set; }

    public IChannel Connection 
    {
        get => base.Connection; 
        set
        {
            base.Connection = value;
            _listener = Task.Run(async () =>
            {
                await foreach (var msg in ReceiveMessagesAsync())
                {
                    if (msg != null)
                        HandleMessage(msg);
                }
            });
        }
    }

    public override bool Connect() =>
        ConnectMethod();

    public async override Task<bool> ConnectAsync() =>
        await ConnectMethodAsync();
}