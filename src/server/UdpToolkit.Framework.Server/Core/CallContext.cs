namespace UdpToolkit.Framework.Server.Core
{
    using UdpToolkit.Core;
    using UdpToolkit.Network.Peers;

    public readonly struct CallContext
    {
        public CallContext(
            byte hubId,
            byte rpcId,
            ushort roomId,
            UdpMode udpMode,
            byte[] payload,
            Peer peer)
        {
            Payload = payload;
            Peer = peer;
            HubId = hubId;
            RpcId = rpcId;
            RoomId = roomId;
            UdpMode = udpMode;
        }

        public byte HubId { get; }

        public byte RpcId { get; }

        public ushort RoomId { get; }

        public UdpMode UdpMode { get; }

        public byte[] Payload { get; }

        public Peer Peer { get; }
    }
}