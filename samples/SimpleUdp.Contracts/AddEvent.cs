using MessagePack;
using UdpToolkit.Annotations;

namespace SimpleUdp.Contracts
{
    [MessagePackObject]
    [ProducedEvent(hubId: 0, rpcId: 0, udpChannel: UdpChannel.Udp)]
    public class AddEvent
    {
        [Key(0)]
        public int X { get; set; }
        
        [Key(1)]
        public int Y { get; set; }
    }
}