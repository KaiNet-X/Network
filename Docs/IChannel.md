# IChannel
Interface for channel types

#### Properties
- `bool Connected { get; }` -Feedback weather this channel is connected or not. Is up to the implementation to use.

#### Methods
- `void SendBytes(byte[] data)` - Send bytes
- `void SendBytes(ReadOnlySpan<byte> data)` - Send bytes
- `Task SendBytesAsync(byte[] data, CancellationToken token = default)` - Send bytes
- `Task SendBytesAsync(ReadOnlyMemory<byte> data, CancellationToken token = default)` - Receive bytes
- `byte[] RecieveBytes()` - Receive bytes
- `Task<byte[]> RecieveBytesAsync(CancellationToken token = default)` - Receive bytes
- `int ReceiveToBuffer(byte[] buffer)` - Similar to how data is normally received on sockets, can offer optimizations
- `Task<int> ReceiveToBufferAsync(byte[] buffer, CancellationToken token = default)` - Similar to how data is normally received on sockets, can offer optimizations