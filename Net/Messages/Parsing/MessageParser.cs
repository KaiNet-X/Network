namespace Net.Messages.Parsing;

using Internals;
using Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Default message parsing algorithm
/// </summary>
public class MessageParser : IMessageParser
{
    private readonly CryptographyService _cryptographyService;
    private readonly ISerializer _serializer;

    /// <summary>
    /// The default constructor
    /// </summary>
    /// <param name="crypto"></param>
    /// <param name="serializer"></param>
    public MessageParser(CryptographyService crypto, ISerializer serializer)
    {
        _cryptographyService = crypto;
        _serializer = serializer;
    }
    
    /// <inheritdoc />
    public byte[] EncapsulateMessage(MessageBase message, Dictionary<string, string> options)
    {
        const int s = sizeof(int);
        
        ReadOnlySpan<byte> serialized = _serializer.Serialize(message, message.GetType());
        
        var enc = options["Encryption"];
        serialized = enc switch
        {
            "Rsa" => _cryptographyService.EncryptRSA(serialized),
            "Aes" => _cryptographyService.EncryptAES(serialized),
            _ => serialized
        };
        
        ReadOnlySpan<byte> header = Encoding.UTF8.GetBytes(message.MessageType);
        
        var bytes = new byte[s * 2 + header.Length + serialized.Length];
        var byteSpan = bytes.AsSpan();
        
        BitConverter.GetBytes(header.Length).CopyTo(byteSpan);
        BitConverter.GetBytes(serialized.Length).CopyTo(byteSpan[s..]);
        header.CopyTo(byteSpan[(s * 2)..]);
        serialized.CopyTo(byteSpan[(header.Length + s * 2)..]);

        return bytes;
    }

    private MessageBase DecapsulateNext(List<byte> bytes, Dictionary<string, string> options, out int read)
    {
        const int s = sizeof(int);

        if (bytes.Count < 2 * s)
        {
            read = 0;
            return null;
        }
        
        ReadOnlySpan<byte> span = CollectionsMarshal.AsSpan(bytes);
        
        var headerLength = BitConverter.ToInt32(span[..s]);
        span = span[4..];
        
        var messageLength = BitConverter.ToInt32(span[..s]);
        span = span[4..];

        if (bytes.Count < 4 + headerLength + messageLength) 
        {
            read = 0;
            return null;
        }
        
        var header = Encoding.UTF8.GetString(span[..headerLength]);
        var type = TypeHandler.RegisteredMessages[header];
        
        span = span[headerLength..];

        var msg = span[..messageLength];
        
        var enc = options["Encryption"];
        msg = enc switch
        {
            "Rsa" => _cryptographyService.DecryptRSA(msg),
            "Aes" => _cryptographyService.DecryptAES(msg),
            _ => msg
        };
        
        read = headerLength + messageLength + 2 * s;
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
        while (true)
        {
            var msg = DecapsulateNext(bytes, options, out var read);
            if (msg is null)
                yield break;
            
            bytes.RemoveRange(0, read);
            yield return msg;
        }
    }
}