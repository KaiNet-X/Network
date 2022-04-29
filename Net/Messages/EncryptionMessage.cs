namespace Net.Messages;

using MessagePack;
using System.Security.Cryptography;

[Attributes.RegisterMessageAttribute]
class EncryptionMessage : MpMessage
{
    public Stage stage { get; set; }
    public RSAParameters RSA { get; set; }
    public byte[] AES { get; set; }

    public EncryptionMessage(RSAParameters param)
    {
        stage = Stage.SYN;
        RSA = param;
        Content = MessagePackSerializer.Serialize(param, ResolveOptions);
    }

    public EncryptionMessage(byte[] param)
    {
        stage = Stage.ACK;
        Content = AES = param;
    }

    public EncryptionMessage(Stage stage)
    {
        this.stage = stage;
    }

    public EncryptionMessage() { }

    protected internal override object GetValue()
    {
        if (stage == Stage.ACK) return Content;
        else return MessagePackSerializer.Deserialize<RSAParameters>(Content, ResolveOptions);
    }

    public enum Stage
    {
        NONE,
        SYN,
        ACK,
        SYNACK
    }
}