namespace Net.Messages;

using Attributes;
using System.Text.Json.Serialization;

[RegisterMessage]
public sealed class SettingsMessage : MessageBase
{
    public NetSettings Settings { get; set; }

    public SettingsMessage(NetSettings settings)
    {
        Settings = settings;
    }

    [JsonConstructor]
    public SettingsMessage() { }
}