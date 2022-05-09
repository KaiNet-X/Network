# MessageBase

The abstract base class all messages inherrit from. Clients use messages to transfer data and manage the connection.

#### Fields/Properties

- `readonly string MessageType` - Typename of derived message class

#### Methods

- `virtual byte[] Serialize()` - Serializes the message into bytes
- `virtual async Task<byte[]> SerializeAsync(CancellationToken token)` - Serializes the message into bytes