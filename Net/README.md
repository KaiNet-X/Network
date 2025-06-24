# KaiNet.Net
A highly customizable networking library

### Functionality
- Send objects been hosts and strongly typed generic event handlers to receive them
Life cycle events
- Managed channels send raw byte data between hosts. Currently there are TCP, Encrypted TCP, and UDP channels. Other channels can be registered on the client and server.
Security
- KaiNet.Net implements optional encryption, starting by exchanging RSA keys and switching over to AES for performance. In addition, object types are whitelisted by registering event handlers for a type. There is an event for object errors to detect when an unregistered object is sent.

### Extensibility
Object serializers can be configured/replaced, and the underlying protocol can also be changed. Custom client and channel types can also be configured.

The [documentation](https://github.com/KaiNet-X/Network) can be found on GitHub.