namespace Net.Connection.Clients.Generic;

using Channels;
using Net.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// The out-of-the-box ServerClient is similar to the Client class, but it is designed to work on the server-side.
/// </summary>
public abstract class ServerClient<MainConnection> : ObjectClient<MainConnection>, IServerClient where MainConnection : BaseChannel
{
    Task IServerClient.connectedTask => Connected.Task;

    private readonly IAsyncEnumerator<MessageBase> _receiver;

    protected ServerClient()
    {
        _receiver = ReceiveMessagesAsync().GetAsyncEnumerator();
    }

    /// <summary>
    /// If the control loop fails, get the exception
    /// </summary>
    public Exception ControlLoopException { get; protected set; }

    internal async Task ReceiveAsync()
    {
        await foreach (var message in ReceiveMessagesAsync()) 
            await HandleMessageAsync(message);
    }

    async Task IServerClient.ReceiveNextAsync()
    {
        try
        {
            var msg = _receiver.Current;

            if (msg != null)
                await HandleMessageAsync(msg);

            await _receiver.MoveNextAsync();
        }
        catch (Exception ex)
        {
            ControlLoopException = ex;
            throw;
        }
    }

    void IServerClient.SetRegisteredObjectTypes(HashSet<Type> registeredTypes) =>
        WhitelistedObjectTypes = registeredTypes;
}