using MessagePack;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Net.Messages
{
    class EncryptionMessage : MpMessage
    {
        public override string MessageType => "encryption";

        public Stage stage { get; set; }
        public RSAParameters RSA { get; set; }
        public byte[] AES { get; set; }

        public EncryptionMessage(RSAParameters param)
        {
            RegisterMessage();
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
}
