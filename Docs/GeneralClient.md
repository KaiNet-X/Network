# GeneralClient\<MainChannel\> : BaseClient where MainChannel : class, IChannel

This client is the base class of ObjectClient\<MainChannel\>. This class implements the message protocol layer using the main connection channel. Also, the handshake is implemented by this class. This class can be inherrited to create a client that uses any underlying connection protocol provided by the MainChannel. For example, this could be adapted to send data via bluetooth or websockets.

`using Net.Connection.Clients.Generic;`

#### Fields/Properties:
- `ConnectState ConnectionState` - State of the connection
- `List<IChannel> Channels` - List of channels

#### Events/Deleages:
- `event Action<object> OnReceiveObject` - Called when an object is received
- `event Action<IChannel> OnChannelOpened` - Called when a channel is opened
- `event Action<bool> OnDisconnect` - Called when disconnected from
- `Action<MessageBase> OnUnregisteredMessage` - Called when there is an unregistered message

#### Methods:
- `void SendObject<T>(T obj)` - Sends an object to the server
- `async Task SendObjectAsync<T>(T obj, CancellationToken token = default)` - Sends an object to the server
- `void SendMessage(MessageBase msg)` - Sends a message to the server
- `async Task SendMessageAsync(MessageBase msg)` - Sends a message to the server
- `void Close()` - Closes the connection
- `void CloseAsync()` - Closes the connection
- `void RegisterMessageHandler<T>(Action<T> handler) where T : MessageBase` - Registers handler for a custom message type
- `void RegisterMessageHandler(Action<MessageBase> handler, Type messageType)` - Registers handler for a custom message type
