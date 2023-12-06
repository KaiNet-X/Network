namespace Net.Connection.Clients.Generic;

using Channels;
using System;
using System.Net;
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

    protected Task Listener { get; set; }

    /// <summary>
    /// Connect to the server this client is bound to
    /// </summary>
    /// <returns>true if connected, otherwise false</returns>
    public abstract bool Connect();

    /// <summary>
    /// Connect to the server this client is bound to
    /// </summary>
    /// <returns>true if connected, otherwise false</returns>
    public abstract Task<bool> ConnectAsync();

    private void StartLoop()
    {
        Listener = Task.Run(async () =>
        {
            await foreach (var msg in ReceiveMessagesAsync())
            {
                if (msg != null)
                    await HandleMessageAsync(msg);
            }
        });
    }
}

public class Client : Client<IChannel>
{
    public Func<bool> ConnectMethod;
    public Func<Task<bool>> ConnectMethodAsync;
    public Action CloseMethod;
    public Func<Task> CloseMethodAsync;

    new public IChannel Connection 
    {
        get => base.Connection; 
        protected set
        {
            base.Connection = value;
            Listener = Task.Run(async () =>
            {
                await foreach (var msg in ReceiveMessagesAsync())
                {
                    if (msg != null)
                        await HandleMessageAsync(msg);
                }
            });
        }
    }

    public override bool Connect() =>
        ConnectMethod();

    public async override Task<bool> ConnectAsync() =>
        await ConnectMethodAsync();

    public override void Close()
    {
        CloseMethod();
        base.Close();
    }

    public override async Task CloseAsync()
    {
        await CloseMethodAsync();
        await base.CloseAsync();
    }

    private protected override void CloseConnection()
    {
        // Do nothing
    }
}