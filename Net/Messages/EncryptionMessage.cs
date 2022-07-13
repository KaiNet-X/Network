namespace Net.Messages;

using System.Security.Cryptography;
using Attributes;

[RegisterMessage]
public sealed class EncryptionMessage : MessageBase
{
    public Stage stage { get; set; }
    public RSAParameters RSA { get; set; }
    public byte[] AES { get; set; }

    public EncryptionMessage(RSAParameters param)
    {
        stage = Stage.SYN;
        RSA = param;
    }

    public EncryptionMessage(byte[] param)
    {
        stage = Stage.ACK;
        AES = param;
    }

    public EncryptionMessage(Stage stage)
    {
        this.stage = stage;
    }

    public EncryptionMessage() { }

    public enum Stage
    {
        NONE,
        SYN,
        ACK,
        SYNACK
    }
}