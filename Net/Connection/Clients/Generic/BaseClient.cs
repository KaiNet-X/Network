namespace Net.Connection.Clients.Generic;

using Messages;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Base class for all clients. Has channels, methods to send and receive messages, work with channels, and close the connection.
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
    public abstract Task SendMessageAsync(MessageBase message, CancellationToken token = default);

    /// <summary>
    /// Lazily receives messages
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerable<MessageBase> ReceiveMessages();

    /// <summary>
    /// Asynchronously and lazily receives messages
    /// </summary>
    /// <returns></returns>
    protected abstract IAsyncEnumerable<MessageBase> ReceiveMessagesAsync();

    /// <summary>
    /// Opens a channel on the client
    /// </summary>
    /// <returns></returns>
    //public abstract IChannel OpenChannel();

    /// <summary>
    /// Opens a channel on the client
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    //public abstract Task<IChannel> OpenChannelAsync(CancellationToken token = default);

    /// <summary>
    /// Closes a channel. This should handle all disposing of the channel and dependency within the client
    /// </summary>
    /// <param name="c"></param>
    //public abstract void CloseChannel(IChannel c);

    /// <summary>
    /// Closes a channel. This should handle all disposing of the channel and dependency within the client
    /// </summary>
    /// <param name="c"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    //public abstract Task CloseChannelAsync(IChannel c, CancellationToken token = default);

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