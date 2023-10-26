using System.Collections;

namespace FileClient;

// This structure represents the folder structure
public class Tree : IEnumerable<Tree>
{
    public string Value { get; set; } = string.Empty;
    public List<Tree> Nodes { get; set; } = new List<Tree>();

    public IEnumerator<Tree> GetEnumerator() => Nodes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}