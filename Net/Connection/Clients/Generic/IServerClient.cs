namespace Net.Connection.Clients.Generic;

using System.Threading.Tasks;

public interface IServerClient
{
    protected internal Task connectedTask { get; }
    internal Task ReceiveNextAsync();
}