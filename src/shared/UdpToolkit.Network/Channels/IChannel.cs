namespace UdpToolkit.Network.Channels
{
    public interface IChannel
    {
        bool HandleInputPacket(
            ushort id,
            uint acks);

        void HandleOutputPacket(
            out ushort id,
            out uint acks);

        bool HandleAck(
            ushort id,
            uint acks);

        bool IsDelivered(
            ushort id);
    }
}