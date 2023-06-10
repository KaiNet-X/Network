namespace Net.Messages.Parsing;

using System;
using System.Collections.Generic;

/// <summary>
/// Interface that provides generic client with methods to parse messages
/// </summary>
public interface IMessageParser
{
    /// <summary>
    /// Encapsulate message into a span of bytes
    /// </summary>
    /// <param name="message">Message to be parsed</param>
    /// <param name="options">Options that a client can pass to the parser</param>
    /// <returns>Span of bytes representing the encoded message</returns>
    public ReadOnlySpan<byte> EncapsulateMessageAsSpan(MessageBase message, Dictionary<string, string> options);

    /// <summary>
    /// Encapsulate message into byte memory
    /// </summary>
    /// <param name="message">Message to be parsed</param>
    /// <param name="options">Options that a client can pass to the parser</param>
    /// <returns>Memory of bytes representing the encoded message</returns>
    public ReadOnlyMemory<byte> EncapsulateMessageAsMemory(MessageBase message, Dictionary<string, string> options);

    /// <summary>
    /// Streams messages from a list of bytes
    /// </summary>
    /// <param name="bytes">Source of encoded messages</param>
    /// <param name="options">Options that a client can pass to the parser</param>
    /// <returns>Sequence of messages</returns>
    public IEnumerable<MessageBase> DecapsulateMessages(List<byte> bytes, Dictionary<string, string> options);
}
