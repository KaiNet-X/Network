using MessagePack;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Net.Messages
{
    [Attributes.RegisterMessageAttribute]
    class SettingsMessage : MpMessage
    {
        public override string MessageType => GetType().Name;
        public SettingsMessage(NetSettings settings)
        {
            //RegisterMessage<SettingsMessage>();
            //RegisterMessage();
            Content = MessagePackSerializer.Serialize(settings, ResolveOptions);
        }

        [JsonConstructor]
        public SettingsMessage() { }

        protected internal override object GetValue()
        {
            return GetValue(typeof(NetSettings));
        }
    }
}
