namespace Net.Messages;

using Attributes;
using Net.Connection.Servers;
using System.Text.Json.Serialization;

[RegisterMessage]
public sealed class SettingsMessage : MessageBase
{
    public ServerSettings Settings { get; set; }

    public SettingsMessage(ServerSettings settings)
    {
        Settings = settings;
    }

    [JsonConstructor]
    public SettingsMessage() { }
}