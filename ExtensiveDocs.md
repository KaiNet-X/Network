# Docs

### Most important:
- [Server](https://github.com/KaiNet-X/Network/blob/master/Server.md)
- [Client](https://github.com/KaiNet-X/Network/blob/master/Client.md)
- [ServerClient](https://github.com/KaiNet-X/Network/blob/master/ServerClient.md)
- [Channel](https://github.com/KaiNet-X/Network/blob/master/Channel.md)

### Extensibility

This library is meant to be highly extensible, so inherritance is commonly used. Out of the box, the server and client are the only classes needed to get a connection going, but they inherrit from base classes which can be used to create custom classes to manage connections.

## Servers

- [Server](https://github.com/KaiNet-X/Network/blob/master/Server.md) - Manages multiple connections with clients (as ServerClient objects)
- [BaseServer]() - Base class for all servers

## Clients

- [Client](https://github.com/KaiNet-X/Network/blob/master/Client.md) - Connects to a server and allows sending of regular objects and messages to and from a server; Ability to manage channels
- [ServerClient](https://github.com/KaiNet-X/Network/blob/master/ServerClient.md) - Server-side version of a client 
- [ObjectClient]() - A client that allows sending of objects and usage of channels; base class for Client and ServerClient
- [GeneralClient]() - Manages connectivity through sockets between client and server; base class for ObjectClient
- [BaseClient]() - Abstract base class for all clients that contain methods for working with messages and channels, as well as closing the connection

## Channels

- [Channel](https://github.com/KaiNet-X/Network/blob/master/Channel.md) - UDP channel 
- [IChannel]() - Methods for sending and receiving raw bytes 

## Messages

- [MessageBase]() - Base class for all messages
- [ChannelManagementMessage]() - Used for managing channels between clients (don't directly use with default implementation)
- [ConfirmationMessage]() - Used in the process of negotiating a connection- subject to change (don't directly use with default implementation)
- [ConnectionPollMessage]() - Used for polling the connection to check for ungraceful disconnections (don't directly use with default implementation)
- [EncryptionMessage]() - Used during the encryption handshake (don't directly use with default implementation)
- [SettingsMessage]() - Used to negotiate settings for the connection (server's choice) (don't directly use with default implementation)
- [ObjectMessage]() - Encapsulates an object and allows for transfer between clients

[MessageParser]() - Used to parse messages from raw byte data

[MessageTypeChecker]() - Used to determine the type of a message before deserialization