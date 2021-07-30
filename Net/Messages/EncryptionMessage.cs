using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Net.Messages
{
    class EncryptionMessage : MessageBase
    {
        public override string MessageType => "encryption";
        public Stage stage { get; set; }

        public EncryptionMessage(Stage stage, RSAParameters param)
        {
            RegisterMessage<EncryptionMessage>();
            this.stage = stage;
            switch (stage)
            {
                case Stage.SYN:
                    Content = MessagePackSerializer.Serialize(param, ResolveOptions);
                    break;
                case Stage.ACK:
                    break;
                case Stage.SYNACK:

                    break;
            }
        }
        public EncryptionMessage(Stage stage, byte[] param)
        {
            this.stage = stage;
            switch (stage)
            {
                case Stage.SYN:
                    break;
                case Stage.ACK:
                    Content = param;
                    break;
                case Stage.SYNACK:
                    break;
            }
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
