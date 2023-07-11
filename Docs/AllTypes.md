# Main types:

### Clients:

- Client : ObjectClient
- ServerClient : ObjectClient
- ObjectClient : ObjectClient\<TcpChannel\>
- ObjectClient\<MainChannel\> : GeneralClient\<MainChannel\> where MainChannel : class, IChannel
- GeneralClient\<MainChannel\> : BaseClient where MainChannel : class, IChannel
- BaseClient

##### Other clients (configurable connections):

*Note these are experimental and haven't been thoroughly tested and developed.*

- GeneralClient\<IChannel\>
- ServerClient\<MainConnection\> ServerClient\<MainConnection\> : ObjectClient\<MainConnection\> where MainConnection : class, IChannel
- ServerClient : ServerClient\<IChannel\>
- Client\<MainConnection\> : ObjectClient\<MainConnection\> where MainConnection : class, IChannel
- Client : Client\<IChannel\>

### Servers:

- Server : BaseServer\<ServerClient\>
- BaseServer\<TClient\> where TClient : BaseClient

##### Other servers (configurable connections):

*Note these are experimental and haven't been thoroughly tested and developed.*

- Server\<ConnectionType\> : BaseServer\<ServerClient\<ConnectionType\>\> where ConnectionType : class, IChannel

- ServerSettings

### Channels

- IChannel
- TcpChannel : IChannel, IDisposable
- UdpChannel : IChannel, IDisposable
- EncryptedTcpChannel : IChannel, IDisposable

### Serialization

Used to customize how the library serializes data. 

- ISerializer
- JSerializer (newtonsoft json)
- MpSerializer (nuecc message pack)

### Message parsing

Used to control the message protocol implemented in GeneralClient and all inherrited classes.

- IMessageParser
- NewMessageParser

### Messages

- MessageBase
- ConfirmationMessage
- ConnectionPollMessage
- DisconnectMessage
- EncryptionMessage
- ChannelManagementMessage
- EncryptionMessage
- ObjectMessage
- SettingsMessage

### Others:

- DisconnectionInfo
- GuardedList\<T\> : IEnumerable\<T\>
- Consts
- ConnectionInfo
