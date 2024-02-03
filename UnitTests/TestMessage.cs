namespace UnitTests;

using Net.Messages;

public class TestMessage : MessageBase
{
    public Guid Guid { get; set; }
    public string Name { get; set; }
}