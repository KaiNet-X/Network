# Server : BaseServer<ServerClient>
The out-of-the-box server implementation allows sending objects, directly accessing ServerClient objects, and follows an event-based approach for receiving data.
#### Constructors

- `Server(IPAddress address, int port, ushort? maxClients = null, NetSettings settings = null)` - Server that listens on one address/port combo
- `Server(IPEndPoint endpoint, ushort? maxClients = null, NetSettings settings = null)` - Server that listens on one address/port combo
- `Server(List<IPEndPoint> endpoints, ushort? maxClients = null, NetSettings settings = null)` - Server that listens on multiple address/port combos

#### Fields/Properties

- `bool Active { get; private set; }` - True when the server is listening and managing connections
- `bool Listening { get; private set; }` - True when the server is listening for new connections
- `bool RemoveAfterDisconnect` Automatically remove a client from Clients after it disconnects
- `List<ServerClient> Clients`- List of clients
- `ushort MaxClients` - Maximum number of allowed connections at a given time
- `ushort LoopDelay` - Delay between client updates; highly reduces CPU usage
- `readonly List<IPEndPoint> Endpoints` - Endpoints passed to the server as arguements (NOTE: this doesn't necessarily represent what the sockets are listening on due to the chance of port being 0 (sets the port num automatically))
- `List<IPEndPoint> ActiveEndpoints` - Endpoints that are currently being listened to by the active sockets (NOTE: will be empty if there aren't any active listening sockets)
- `readonly NetSettings Settings` - Settings
- `Dictionary<string, Action<MessageBase, ServerClient>> CustomMessageHandlers` - Handlers for custom messages

#### Events/Delegates

- `event Action<Channel, ServerClient> OnClientChannelOpened` - Inkvoked when a new channel is opened
- `event Action<object, ServerClient> OnClientObjectReceived` - Invoked when an object gets recieved
- `event Action<ServerClient> OnClientConnected` - Invoked when a client connects
- `event Action<ServerClient, bool> OnClientDisconnected` - Invoked when a client disconnects
- `event Action<MessageBase, ServerClient> OnUnregisteredMessage` Invoked when an unregistered message is received

#### Methods
- `void SendObjectToAll<T>(T obj)` - Sends an object of any type to all clients
- `async Task SendObjectToAllAsync<T>(T obj)` - Sends an object of any type to all clients
- `void SendMessageToAll(MessageBase msg)` - Sends a message to all clients
- `async Task SendMessageToAllAsync(MessageBase msg)` - Sends a message to all clients
- `void Start()` - Starts the server and listens for incoming connections
- `void StartAsync()` - Starts the server and listens for incoming connections
- `void Stop()` - Stops listening for incoming connections
- `async Task StopAsync()` - Stops listening for incoming connections
- `void ShutDown()` - Stops listening and closes/removes all clients
- `async Task ShutDownAsync()` - Stops listening and closes/removes all clients
- `void RegisterChannelType<T>(Func<ServerClient, Task<T>> open, Func<ChannelManagementMessage, ServerClient, Task> channelManagement, Func<T, ServerClient, Task> close) where T : IChannel` - Registers a custom channel on the server (Must also be done on the client to work)