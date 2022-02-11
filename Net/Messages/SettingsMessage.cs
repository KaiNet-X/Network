using MessagePack;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Net.Messages
{
    class SettingsMessage : MessageBase
    {
        public override string MessageType => "settings";
        public SettingsMessage(NetSettings settings)
        {
            //RegisterMessage<SettingsMessage>();
            RegisterMessage();
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
