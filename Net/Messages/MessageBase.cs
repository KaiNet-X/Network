namespace Net.Messages;

/// <summary>
/// Base class for all message types
/// </summary>
public abstract class MessageBase
{
    private string _messageType;

    /// <summary>
    /// Gets the type of the message. This is used in the message protocol.
    /// </summary>
    public string MessageType => 
        _messageType ??= GetType().Name;
}