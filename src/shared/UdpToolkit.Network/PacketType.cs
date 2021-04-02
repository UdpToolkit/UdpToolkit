namespace UdpToolkit.Network
{
    public enum PacketType : byte
    {
        FromClient = 0,
        Protocol = 1,
        Ack = 2,
        FromServer = 3,
    }
}