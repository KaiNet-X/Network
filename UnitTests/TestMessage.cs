namespace UnitTests;

using Net.Attributes;
using Net.Messages;

[RegisterMessage]
public class TestMessage : MessageBase
{
    public Guid Guid { get; set; }
    public string Name { get; set; }
}
