namespace UdpToolkit.Network.Clients
{
    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Serilog;
    using UdpToolkit.Network.Packets;

    public sealed class UdpReceiver : IUdpReceiver
    {
        private readonly UdpClient _receiver;
        private readonly IUdpProtocol _udpProtocol;

        private readonly ILogger _logger = Log.ForContext<UdpReceiver>();

        public UdpReceiver(
            UdpClient receiver,
            IUdpProtocol udpProtocol)
        {
            _receiver = receiver;
            _udpProtocol = udpProtocol;
            _logger.Debug($"{nameof(UdpReceiver)} - {receiver.Client.LocalEndPoint} created");
        }

        public event Action<NetworkPacket> UdpPacketReceived;

        public async Task StartReceiveAsync()
        {
            while (true)
            {
                var result = await _receiver
                    .ReceiveAsync()
                    .ConfigureAwait(false);

                _logger.Debug($"Packet from - {result.RemoteEndPoint} to {_receiver.Client.LocalEndPoint} received");

                var networkPacket = _udpProtocol.Deserialize(
                        bytes: new ArraySegment<byte>(result.Buffer),
                        ipEndPoint: result.RemoteEndPoint);

                UdpPacketReceived?.Invoke(networkPacket);
            }
        }

        public void Dispose()
        {
            _receiver.Dispose();
        }
    }
}
