# How it works

UdpToolkit consists of two main parts:
- Network for working with bytes, IP addresses, custom network headers e.t.c. 
- Framework for handle user-defined events like `Move`, `Shot`, `Heal`, `Join` e.t.c.   

## Network

### Network protocol

`|protocol header|payload|`

### Protocol header

| Number  | Name         | Type        | Size in bytes | Description                                                                                                                                     |
| ------- | ------------ | ----------- | ------------- |------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1.      | ChannelId    | byte        | 1             | Identifier of channel.                                                                                                                          |
| 2.      | Id           | ushort      | 2             | Identifier of current packet.                                                                                                                   |
| 3.      | Acks         | uint        | 4             | 32 bit with info about previous packets relative current. 1bit per packet. Reserved, not implemented.                                           |
| 4.      | ConnectionId | Guid        | 16            | Identifier of connection.                                                                                                                       |
| 5.      | PacketType   | PacketType  | 1             | Packet type (bit flag).                                                                                                                         |
| 6.      | DataType     | byte        | 1             | Type of user-defined event/subscription. Used for serialization/deserialization and support counter per each event in the sequenced channels.   |

### Internal protocol

| Type               | Size in bytes | Description                                     | Behaviour                                                                                     |
| ------------------ | ------------- |------------------------------------------------ | --------------------------------------------------------------------------------------------- |
| Connect            | 24            | Incoming connection packet.                     | Send by the client, to initiate the connection.                                               |
| Connect OR Ack     | 24            | Acknowledge for connection packet.              | Send by the server if connection is established.                                              |
| Disconnect         | 24            | Incoming disconnect packet.                     | Send by the client, to explicit disconnection.                                                |                                                             
| Disconnect OR Ack  | 24            | Acknowledge for disconnect packet.              | Send by the server if client disconnected.                                                    |
| Heartbeat          | 24            | Incoming heartbeat packet.                      | Send by client for two reasons: 1) RTT measurement, 2) Initiate resending of pending packets. |
| Heartbeat OR Ack   | 24            | Acknowledge for heartbeat packet.               | Send by the server if heartbeat packet received.                                              |
| UserDefined        | 24            | Incoming user-defined packet.                   | Send by client.                                                                               |
| UserDefined OR Ack | 24            | Acknowledge for user-defined packet.            | Send by server if user-defined packet received.                                               |
| Ack                | 24            | Acknowledge bit, could be added to any packets. | Added for all incoming reliable packets.                                                      |

### Expansion points

`ISocket` - For performance reasons default sockets in `UdpToolkit` are written in C, but very difficult to support this solution for all possible platforms. Fallback on managed version always available.

`IChannel` - Unfortunately, no silver bullet for reliable UDP protocols if you want to implement your own mechanism for working with the `Protocol header` you could implement a custom channel. All actions in the scope of the channel are thread-safe.

## Framework

### Framework protocol (Implicit)

The client should include `GroupId:Guid` (16 bytes) in each event. This is required to restrict broadcasting scope on the framework side.

### Concurrency

All actions performed in `onEvent` callback are not thread-safe. This is absolutely the same behavior as in popular web frameworks for .net like `asp.net core` or `GRPC`.

Note: But each connection deterministically dispatches into the same thread and all events from this connection are processed by the same thread.
```c#
host.On<Message>(
    onEvent: (connectionId, ip, message) =>
    {
        // not thread-safe here
        Console.WriteLine($"Message received: {message.Text}");
    });
```

For thread-safe events processing in-room scope, you could use other libraries or implement locking the room by yourself.