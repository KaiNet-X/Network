namespace Net.Messages;

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MessageParser
{
    static readonly byte[] Start = new byte[] { 0x3c, 0x53, 0x54, 0x41, 0x3e };
    static readonly byte[] End = new byte[] { 0x3c, 0x45, 0x4e, 0x44, 0x3e };

    public static byte[] Encapsulate(byte[] bytes)
    {
        int l1 = Start.Length + bytes.Length;
        int l2 = l1 + End.Length;

        byte[] b = new byte[l2];

        Buffer.BlockCopy(Start, 0, b, 0, Start.Length);
        Buffer.BlockCopy(bytes, 0, b, Start.Length, bytes.Length);
        Buffer.BlockCopy(End, 0, b, Start.Length + bytes.Length, End.Length);

        //for (int i = 0; i < Start.Length; i++)
        //    b[i] = Start[i];

        //for (int i = Start.Length; i < l1; i++)
        //    b[i] = bytes[i - Start.Length];

        //for (int i = l1; i < l2; i++)
        //    b[i] = End[i - l1];

        return b;
    }

    public static byte[] RemoveTags(List<byte> b)
    {
        int start = Utilities.IndexInByteArray(b, Start);
        int end = Utilities.IndexInByteArray(b, End);

        if (end == -1 || start == -1)
            return new byte[0];

        List<byte> sub = b.GetRange(start + Start.Length, end - start - Start.Length);
        b.RemoveRange(start, end - start + End.Length);
        return sub.ToArray();
    }

    #region GetMessages
    public static List<MessageBase> GetMessages(List<byte> obj)
    {
        byte[] sub = null;
        List<MessageBase> msg = new List<MessageBase>();

        while (true)
        {
            sub = RemoveTags(obj);
            if (sub.Length == 0) break;

            msg.Add(Deserialize(sub));
            if (obj.Count == 0) break;
        }
        return msg;
    }

    public static List<MessageBase> GetMessagesAes(List<byte> obj, byte[] encKey)
    {
        byte[] sub = null;
        List<MessageBase> msg = new List<MessageBase>();
        while (true)
        {
            sub = RemoveTags(obj);
            if (sub.Length == 0) break;

            sub = CryptoServices.DecryptAES(sub, encKey, encKey);

            msg.Add(Deserialize(sub));
            if (obj.Count == 0) break;
        }
        return msg;
    }

    public static List<MessageBase> GetMessagesRsa(List<byte> obj, RSAParameters encKey)
    {
        byte[] sub = null;
        List<MessageBase> msg = new List<MessageBase>();

        while (true)
        {
            sub = RemoveTags(obj);
            if (sub.Length == 0) break;

            sub = CryptoServices.DecryptRSA(sub, encKey);

            msg.Add(Deserialize(sub));
            if (obj.Count == 0) break;
        }
        return msg;
    }

    #endregion
    #region Serialization

    public static MessageBase Deserialize(byte[] obj)
    {
        byte[] start = new byte[] { 0x7b, 0x7b };
        byte[] end = new byte[] { 0x7d, 0x7d };

        int s = Utilities.IndexInByteArray(obj, start);
        int e = Utilities.IndexInByteArray(obj, end, 2);

        string type = Encoding.UTF8.GetString(obj[2..(e)]);

        Type t = MessageBase.Registered[type];

        return MessageBase.Deserialize(obj[(e + 2)..^0], t);
    }

    public static byte[] Serialize(MessageBase message)
    {
        var serializer = MessageBase.Serializer;
        byte[] serialized = serializer.Serialize(message, message.GetType());
        byte[] bytes = new byte[serialized.Length + message.MessageType.Length + 4];
        byte[] b = Encoding.UTF8.GetBytes($"{{{{{message.MessageType}}}}}");

        for (int i = 0; i < b.Length; i++)
            bytes[i] = b[i];

        for (int i = b.Length; i < b.Length + serialized.Length; i++)
            bytes[i] = serialized[i - b.Length];
        
        return bytes;
    }

    public static async Task<byte[]> SerializeAsync(MessageBase message, CancellationToken token)
    {
        var serializer = MessageBase.Serializer;
        
        byte[] serialized = await serializer.SerializeAsync(message, message.GetType(), token);
        byte[] bytes = new byte[serialized.Length + message.MessageType.Length + 4];
        byte[] b = Encoding.UTF8.GetBytes($"{{{{{message.MessageType}}}}}");

        for (int i = 0; i < b.Length; i++)
            bytes[i] = b[i];

        for (int i = b.Length; i < b.Length + serialized.Length; i++)
            bytes[i] = serialized[i - b.Length];

        return bytes;
    }

    #endregion
}
