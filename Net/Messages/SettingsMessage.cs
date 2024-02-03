namespace Net.Messages;

using Net;
using System.Text.Json.Serialization;

public sealed class SettingsMessage : MessageBase
{
    public ConnectionSettings Settings { get; set; }

    public SettingsMessage(ConnectionSettings settings)
    {
        Settings = settings;
    }

    [JsonConstructor]
    public SettingsMessage() { }
}