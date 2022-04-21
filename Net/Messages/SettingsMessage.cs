namespace Net.Messages;

using MessagePack;
using System.Text.Json.Serialization;

[Attributes.RegisterMessageAttribute]
class SettingsMessage : MpMessage
{
    public override string MessageType => GetType().Name;

    public SettingsMessage(NetSettings settings)
    {
        Content = MessagePackSerializer.Serialize(settings, ResolveOptions);
    }

    [JsonConstructor]
    public SettingsMessage() { }

    protected internal override object GetValue() =>
        GetValue(typeof(NetSettings));
}