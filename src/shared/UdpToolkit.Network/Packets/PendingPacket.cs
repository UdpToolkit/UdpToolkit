namespace UdpToolkit.Network.Packets
{
    using System;
    using UdpToolkit.Network.Contracts.Sockets;

    internal readonly struct PendingPacket
    {
        internal PendingPacket(
            byte[] payload,
            IpV4Address to,
            DateTimeOffset createdAt,
            ushort id,
            byte channelId,
            Guid connectionId,
            PacketType packetType)
        {
            Payload = payload;
            To = to;
            CreatedAt = createdAt;
            Id = id;
            ChannelId = channelId;
            ConnectionId = connectionId;
            PacketType = packetType;
        }

        public byte[] Payload { get; }

        public IpV4Address To { get; }

        public ushort Id { get; }

        public byte ChannelId { get; }

        public DateTimeOffset CreatedAt { get; }

        public Guid ConnectionId { get; }

        public PacketType PacketType { get; }
    }
}