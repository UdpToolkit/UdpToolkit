namespace ReliableUdp.Contracts
{
    using System;
    using UdpToolkit.Annotations;

    [UdpEvent]
    public class JoinEvent
    {
        public JoinEvent(
            Guid roomId,
            string nickname)
        {
            RoomId = roomId;
            Nickname = nickname;
        }

        public Guid RoomId { get; }

        public string Nickname { get; }
    }
}