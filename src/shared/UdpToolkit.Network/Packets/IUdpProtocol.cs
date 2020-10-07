namespace UdpToolkit.Network.Packets
{
    using System;
    using System.Net;
    using UdpToolkit.Network.Packets;

    public interface IUdpProtocol
    {
        NetworkPacket Deserialize(
            ArraySegment<byte> bytes,
            IPEndPoint ipEndPoint,
            TimeSpan resendPacketTimeout);

        byte[] Serialize(
            NetworkPacket networkPacket);
    }
}
