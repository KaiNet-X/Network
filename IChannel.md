# IChannel
Interface for channel types

#### Methods
- `void SendBytes(byte[] data)` - Send bytes
- `Task SendBytesAsync(byte[] data, CancellationToken token = default)` - Send bytes
- `byte[] RecieveBytes()` - Receive bytes
- `Task<byte[]> RecieveBytesAsync(CancellationToken token = default)` - Receive bytes