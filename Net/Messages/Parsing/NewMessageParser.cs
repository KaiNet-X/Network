namespace Net.Messages.Parsing;

using Net.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Default message parsing algorithm
/// </summary>
public class NewMessageParser : IMessageParser
{
    private readonly CryptographyService _cryptographyService;
    private readonly ISerializer _serializer;

    public NewMessageParser(CryptographyService crypto, ISerializer serializer)
    {
        _cryptographyService = crypto;
        _serializer = serializer;
    }

    /// <summary>
    /// Encapsulate message into a span of bytes
    /// </summary>
    /// <param name="message">Message to be parsed</param>
    /// <param name="options">Options that a client can pass to the parser</param>
    /// <returns>Span of bytes representing the encoded message</returns>
    public ReadOnlySpan<byte> EncapsulateMessageAsSpan(MessageBase message, Dictionary<string, string> options)
    {
        ReadOnlySpan<byte> serialized = _serializer.Serialize(message, message.GetType());

        var enc = options["Encryption"];
        switch (enc)
        {
            case "Rsa":
                serialized = _cryptographyService.EncryptRSA(serialized);
                break;
            case "Aes":
                serialized = _cryptographyService.EncryptAES(serialized);
                break;
        }

        ReadOnlySpan<byte> header = Encoding.UTF8.GetBytes($"{message.MessageType}}}{serialized.Length}}}}}").AsSpan();
        Span<byte> bytes = new byte[serialized.Length + header.Length];

        header.CopyTo(bytes);
        serialized.CopyTo(bytes.Slice(header.Length));

        return bytes;
    }

    /// <summary>
    /// Encapsulate message into byte memory
    /// </summary>
    /// <param name="message">Message to be parsed</param>
    /// <param name="options">Options that a client can pass to the parser</param>
    /// <returns>Memory of bytes representing the encoded message</returns>
    public ReadOnlyMemory<byte> EncapsulateMessageAsMemory(MessageBase message, Dictionary<string, string> options)
    {
        ReadOnlyMemory<byte> serialized = _serializer.Serialize(message, message.GetType());

        var enc = options["Encryption"];
        switch (enc)
        {
            case "Rsa":
                serialized = _cryptographyService.EncryptRSA(serialized);
                break;
            case "Aes":
                serialized = _cryptographyService.EncryptAES(serialized);
                break;
        }

        Memory<byte> header = Encoding.UTF8.GetBytes($"{message.MessageType}}}{serialized.Length}}}}}").AsMemory();
        Memory<byte> bytes = new byte[serialized.Length + header.Length];

        header.CopyTo(bytes);
        serialized.CopyTo(bytes.Slice(header.Length));

        return bytes;
    }

    private MessageBase DecapsulateNext(List<byte> bytes, Dictionary<string, string> options, ref int origin)
    {
        var span = CollectionsMarshal.AsSpan(bytes).Slice(origin);
        int msgEnd = Utilities.IndexInByteSpan(span, new byte[] { 0x7d });
        int lengthEnd = Utilities.IndexInByteSpan(span, new byte[] { 0x7d, 0x7d });

        if (msgEnd == -1 || lengthEnd == -1)
            return null;

        int len = int.Parse(Encoding.UTF8.GetString(span.Slice(msgEnd + 1, lengthEnd - msgEnd - 1)));

        if (span.Length < 2 + lengthEnd + len)
            return null;

        ReadOnlySpan<byte> msg = span.Slice(2 + lengthEnd, len);

        var enc = options["Encryption"];
        switch (enc)
        {
            case "Rsa":
                msg = _cryptographyService.DecryptRSA(msg);
                break;
            case "Aes":
                msg = _cryptographyService.DecryptAES(msg);
                break;
        }

        var t = span.Slice(0, msgEnd);
        var type = MessageBase.Registered[Encoding.UTF8.GetString(t)];

        origin += lengthEnd + 2 + len;
        return _serializer.Deserialize(msg, type) as MessageBase;
    }

    /// <summary>
    /// Streams messages from a list of bytes
    /// </summary>
    /// <param name="bytes">Source of encoded messages</param>
    /// <param name="options">Options that a client can pass to the parser</param>
    /// <returns>Sequence of messages</returns>
    public IEnumerable<MessageBase> DecapsulateMessages(List<byte> bytes, Dictionary<string, string> options)
    {
        int origin = 0;
        while (true)
        {
            var msg = DecapsulateNext(bytes, options, ref origin);
            if (msg == null)
            {
                bytes.RemoveRange(0, origin);
                yield break;
            };
            yield return msg;
        }
    }
}