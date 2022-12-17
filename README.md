# KaiNet.Net

As the successor to [KaiNet.SimpleNetwork](https://github.com/KaiNet-X/simple-network-library), this library is designed with 
performance in mind while still keeping networking simple.

Net is designed to be highly extensible, containing base classes for a server, client, channels, and messages. The out-of-the-box implementation uses socket communication to send "messages" which encapsulate data, and the default client/server are able to transmit objects using this message protocol. Your program is notified via the OnObjectReceived/OnClientObjectReceived events. 

Channels allow you to open parallel connections to a host to send raw byte data. The two built-in channels use Tcp and Udp respectively.

Click [here](https://github.com/KaiNet-X/Network/blob/master/gettingStarted.md) to get started.

Want to donate? [Buy me a coffee!](https://www.buymeacoffee.com/kainet)

~~For version 2 of the library, In order to simplify the documentation process, I have been working with xml comments and generated md. The old docs will still be available, but note that they won't be getting updated for now.~~

The version 2 docs are listed below, however the V3 docs are in progress and will be replacing the V1 docs.

<table>
<tbody>
<tr>
<td><a href="#registermessageattribute">RegisterMessageAttribute</a></td>
<td><a href="#ichannel">IChannel</a></td>
</tr>
<tr>
<td><a href="#udpchannel">UdpChannel</a></td>
<td><a href="#baseclient">BaseClient</a></td>
</tr>
<tr>
<td><a href="#client">Client</a></td>
<td><a href="#connectstate">ConnectState</a></td>
</tr>
<tr>
<td><a href="#generalclient">GeneralClient</a></td>
<td><a href="#objectclient">ObjectClient</a></td>
</tr>
<tr>
<td><a href="#serverclient">ServerClient</a></td>
<td><a href="#baseserver\`1">BaseServer\`1</a></td>
</tr>
<tr>
<td><a href="#server">Server</a></td>
<td><a href="#messagebase">MessageBase</a></td>
</tr>
<tr>
<td><a href="#netsettings">NetSettings</a></td>
<td><a href="#iserializer">ISerializer</a></td>
</tr>
</tbody>
</table>


## RegisterMessageAttribute

This class registers a message


## IChannel

Interface for all channel types. Channels are meant to send raw data from one endpoint to another

### Close

Closes the channel

### CloseAsync

Closes the channel

### RecieveBytes

Receive bytes on from remote

#### Returns

bytes

### RecieveBytesAsync(System.Threading.CancellationToken)

Receive bytes on from remote

#### Returns

bytes

### SendBytes(data)

Send bytes to remote

| Name | Description |
| ---- | ----------- |
| data | *System.Byte[]*<br> |

### SendBytesAsync(data)

Send bytes to remote

| Name | Description |
| ---- | ----------- |
| data | *System.Byte[]*<br> |


## UdpChannel

This channel is designed to send UDP data between clients. Call SetRemote to connect to a remote endpoing

### Constructor(local, aesKey)

Udp channel bound to an endpoint using an AES encryption key

| Name | Description |
| ---- | ----------- |
| local | *System.Net.IPEndPoint*<br> |
| aesKey | *System.Byte[]*<br> |

### Constructor(local)

Udp channel bound to an endpoint

| Name | Description |
| ---- | ----------- |
| local | *System.Net.IPEndPoint*<br> |

### Connected

If the channel is connected

### Local

Local endpoint

### RecieveBytes

Receive bytes from internal queue

#### Returns



### RecieveBytesAsync(System.Threading.CancellationToken)

Receive bytes from internal queue

#### Returns



### Remote

Remote endpoint

### SetRemote(endpoint)

Sets the remote endpoint and connects to it

| Name | Description |
| ---- | ----------- |
| endpoint | *System.Net.IPEndPoint*<br> |


## BaseClient

Base class for all clients. Has channels, methods to send and receive messages, work with channels, and close the connection.

### Channels

Channels for communication. These are seperate to the main connection and can be used to send raw data.

### Close

Closes the connection

### CloseAsync

Closes the connection

#### Returns



### CloseChannel(c)

Closes a channel. This should handle all disposing of the channel and dependency within the client

| Name | Description |
| ---- | ----------- |
| c | *Net.Connection.Channels.IChannel*<br> |

### CloseChannelAsync(c, token)

Closes a channel. This should handle all disposing of the channel and dependency within the client

| Name | Description |
| ---- | ----------- |
| c | *Net.Connection.Channels.IChannel*<br> |
| token | *System.Threading.CancellationToken*<br> |

#### Returns



### OpenChannel

Opens a channel on the client

#### Returns



### OpenChannelAsync(token)

Opens a channel on the client

| Name | Description |
| ---- | ----------- |
| token | *System.Threading.CancellationToken*<br> |

#### Returns



### ReceiveMessages

Lazily receives messages

#### Returns



### ReceiveMessagesAsync

Asynchronously and lazily receives messages

#### Returns



### SendMessage(message)

Sends a message to the remote client.

| Name | Description |
| ---- | ----------- |
| message | *Net.Messages.MessageBase*<br> |

### SendMessageAsync(message)

Sends a message to the remote client.

| Name | Description |
| ---- | ----------- |
| message | *Net.Messages.MessageBase*<br> |


## Client

The out-of-the-box Client implementation allows sending objects to the server, managing UDP channels, and follows an event based approach to receiving data.

### Constructor(address, port)



| Name | Description |
| ---- | ----------- |
| address | *System.Net.IPAddress*<br>IP address of server |
| port | *System.Int32*<br>Server port the client will connect to |

### Constructor(ep)



| Name | Description |
| ---- | ----------- |
| ep | *System.Net.IPEndPoint*<br>IPEndpoint of the server |

### Constructor(address, port)



| Name | Description |
| ---- | ----------- |
| address | *System.String*<br>IP address of server |
| port | *System.Int32*<br>Server port the client will connect to |

### Connect(maxAttempts, throwWhenExausted)

Connect to the server this client is bound to

| Name | Description |
| ---- | ----------- |
| maxAttempts | *System.Int32*<br>Max amount of connection attempts |
| throwWhenExausted | *System.Boolean*<br>Throw exception if connection didn't work |

#### Returns

true if connected, otherwise false

### ConnectAsync(maxAttempts, throwWhenExausted)

Connect to the server this client is bound to

| Name | Description |
| ---- | ----------- |
| maxAttempts | *System.Int32*<br>Max amount of connection attempts |
| throwWhenExausted | *System.Boolean*<br>Throw exception if connection didn't work |

#### Returns

true if connected, otherwise false

### LoopDelay

Delay between client updates; highly reduces CPU usage


## ConnectState

State of the connection


## GeneralClient

Base class for ObjectClient that impliments the underlying protocol

### ConnectionState

The state of the current connection.

### CustomMessageHandlers

Register message handlers for custom message types with message type name

### LocalEndpoint

Local endpoint

### OnDisconnect

Invoked when disconnected from. Argument is graceful or ungraceful.

### OnUnregisteredMessage

Invoked when an unregistered message is received

### RemoteEndpoint

Remote endpoint


## ObjectClient

Base client for Client and ServerClient that adds functionality for sending/receiving objects.

### OnChannelOpened

Invoked when a channel is opened

### OnReceiveObject

Invoked when the client receives an object

### SendObject\`\`1(obj)

Sends an object to the remote client

#### Type Parameters

- T - Type of the object to be sent

| Name | Description |
| ---- | ----------- |
| obj | *\`\`0*<br>Object |

### SendObjectAsync\`\`1(obj)

Sends an object to the remote client

#### Type Parameters

- T - Type of the object to be sent

| Name | Description |
| ---- | ----------- |
| obj | *\`\`0*<br>Object |


## ServerClient

The out-of-the-box ServerClient is similar to the Client class, but it is designed to work on the server-side.


## BaseServer\`1

Base class for all servers

#### Type Parameters

- TClient - Generic client type. This must inherrit base client, and is used to keep a consistent client implementation on the server.

### SendMessageToAll(msg)

Sends a message to all clients

| Name | Description |
| ---- | ----------- |
| msg | *Net.Messages.MessageBase*<br> |

### SendMessageToAllAsync(msg)

Sends a message to all clients

| Name | Description |
| ---- | ----------- |
| msg | *Net.Messages.MessageBase*<br> |

### ShutDown

Completely shuts the server down and closes all connections

### ShutDownAsync

Completely shuts the server down and closes all connections

### Start

Starts listening for incoming connections

### StartAsync

Starts listening for incoming connections

### Stop

Stops listening for new client connections

### StopAsync

Stops listening for new client connections


## Server

Default server implementation

### Constructor(endpoints, maxClients, settings)

New server object

| Name | Description |
| ---- | ----------- |
| endpoints | *System.Collections.Generic.List{System.Net.IPEndPoint}*<br>List of endpoints for the server to bind to |
| maxClients | *System.Nullable{System.UInt16}*<br>Max amount of clients |
| settings | *Net.NetSettings*<br>Settings for connection |

### Constructor(address, port, maxClients, settings)

New server object

| Name | Description |
| ---- | ----------- |
| address | *System.Net.IPAddress*<br>IP address for the server to bind to |
| port | *System.Int32*<br>Port for the server to bind to |
| maxClients | *System.Nullable{System.UInt16}*<br>Max amount of clients |
| settings | *Net.NetSettings*<br>Settings for connection |

### Constructor(endpoint, maxClients, settings)

New server object

| Name | Description |
| ---- | ----------- |
| endpoint | *System.Net.IPEndPoint*<br>Endpoint for the server to bind to |
| maxClients | *System.Nullable{System.UInt16}*<br>Max amount of clients |
| settings | *Net.NetSettings*<br>Settings for connection |

### Active

If the server is active or not

### CustomMessageHandlers

Handlers for custom message types

### Endpoints

All endpoints the server is accepting connections on

### Listening

If the server is listening for connections

### LoopDelay

Delay between client updates; highly reduces CPU usage

### MaxClients

Max connections at one time

### OnClientChannelOpened

Invoked when a channel is opened on a client

### OnClientConnected

Invoked when a client is connected

### OnClientDisconnected

Invoked when a client disconnects

### OnClientObjectReceived

Invoked when a client receives an object

### OnUnregisteredMessege

Invoked when a client receives an unregistered custom message

### RegisterType\`\`1

Registers an object type. This is used as an optimization before the server sends or receives objects.

#### Type Parameters

- T - 

### SendObjectToAll\`\`1(obj)

Sends an object to all clients

#### Type Parameters

- T - 

| Name | Description |
| ---- | ----------- |
| obj | *\`\`0*<br> |

### SendObjectToAllAsync\`\`1(obj)

Sends an object to all clients

#### Type Parameters

- T - 

| Name | Description |
| ---- | ----------- |
| obj | *\`\`0*<br> |

### Settings

Settings for this server that are set in the constructor


## MessageBase

Base class for all message types

### MessageType

Gets the type of the message. This is used in the message protocol.

### Registered

Dictionary of registered message types. By default is all messages with RegisterMessageAttribute.


## NetSettings

Settings that you can pass to the server

### ConnectionPollTimeout

Timeout for connection checks

### EncryptChannels

Encrypt channels

### SingleThreadedServer

Run serverclients on one thread or dedicated threads

### UseEncryption

Encrypt the main connection


## ISerializer

Serializer used in generalclient

### Deserialize(bytes, type)

Converts bytes to an object

| Name | Description |
| ---- | ----------- |
| bytes | *System.Byte[]*<br> |
| type | *System.Type*<br> |

#### Returns



### DeserializeAsync(bytes, type)

Converts bytes to an object

| Name | Description |
| ---- | ----------- |
| bytes | *System.Byte[]*<br> |
| type | *System.Type*<br> |

#### Returns



### Serialize(obj, type)

Converts an object to bytes

| Name | Description |
| ---- | ----------- |
| obj | *System.Object*<br> |
| type | *System.Type*<br> |

#### Returns



### SerializeAsync(obj, type)

Converts an object to bytes

| Name | Description |
| ---- | ----------- |
| obj | *System.Object*<br> |
| type | *System.Type*<br> |

#### Returns


