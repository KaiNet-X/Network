namespace Net.Connection.Clients.Generic;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IServerClient
{
    protected internal Task connectedTask { get; }
    protected internal Task ReceiveNextAsync();
    protected internal void SetRegisteredObjectTypes(HashSet<Type> registeredTypes);
}