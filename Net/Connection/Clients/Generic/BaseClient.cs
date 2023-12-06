namespace Net.Connection.Clients.Generic;

using Messages;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Base class for all clients. Provices methods to send and receive messages and close the connection.
/// </summary>
public abstract class BaseClient
{
    /// <summary>
    /// Sends a message to the remote client.
    /// </summary>
    /// <param name="message"></param>
    public abstract void SendMessage(MessageBase message);

    /// <summary>
    /// Sends a message to the remote client.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="token"></param>
    public abstract Task SendMessageAsync(MessageBase message, CancellationToken token = default);

    /// <summary>
    /// Asynchronously and lazily receives messages
    /// </summary>
    /// <returns></returns>
    protected abstract IAsyncEnumerable<MessageBase> ReceiveMessagesAsync();

    /// <summary>
    /// Closes the connection
    /// </summary>
    public abstract void Close();

    /// <summary>
    /// Closes the connection
    /// </summary>
    /// <returns></returns>
    public abstract Task CloseAsync();
}