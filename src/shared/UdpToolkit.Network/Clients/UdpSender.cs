namespace UdpToolkit.Network.Clients
{
    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using UdpToolkit.Logging;
    using UdpToolkit.Network.Channels;
    using UdpToolkit.Network.Packets;
    using UdpToolkit.Network.Pooling;
    using UdpToolkit.Network.Utils;

    public sealed class UdpSender : IUdpSender
    {
        private const int MtuSizeLimit = 1500; // TODO detect automatic

        private readonly UdpClient _sender;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IConnectionPool _connectionPool;
        private readonly IUdpToolkitLogger _udpToolkitLogger;

        public UdpSender(
            UdpClient sender,
            IObjectsPool<NetworkPacket> networkPacketPool,
            IUdpToolkitLogger udpToolkitLogger,
            IConnectionPool connectionPool,
            IDateTimeProvider dateTimeProvider)
        {
            _sender = sender;
            _udpToolkitLogger = udpToolkitLogger;
            _connectionPool = connectionPool;
            _dateTimeProvider = dateTimeProvider;
            _udpToolkitLogger.Debug($"{nameof(UdpSender)} - {sender.Client.LocalEndPoint} created");
        }

        public void Dispose()
        {
            _sender.Dispose();
        }

        public async Task SendAsync(
            PooledObject<NetworkPacket> pooledNetworkPacket)
        {
            var connection = _connectionPool
                .TryGetConnection(pooledNetworkPacket.Value.ConnectionId);

            if (connection == null)
            {
                pooledNetworkPacket.Dispose();
                return;
            }

            var networkPacketType = pooledNetworkPacket.Value.NetworkPacketType;

            if (pooledNetworkPacket.Value.IsProtocolEvent)
            {
                switch ((ProtocolHookId)pooledNetworkPacket.Value.HookId)
                {
                    case ProtocolHookId.P2P:
                        break;
                    case ProtocolHookId.Ping when networkPacketType == NetworkPacketType.Protocol:
                        connection?.OnPing(_dateTimeProvider.GetUtcNow());

                        break;
                    case ProtocolHookId.Ping when networkPacketType == NetworkPacketType.Ack:
                        connection?.OnPingAck(_dateTimeProvider.GetUtcNow());

                        break;
                    case ProtocolHookId.Disconnect:
                        break;
                    case ProtocolHookId.Connect:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            SendInternalAsync(connection, pooledNetworkPacket);

            var bytes = NetworkPacket.Serialize(pooledNetworkPacket.Value);

            if (bytes.Length > MtuSizeLimit)
            {
                _udpToolkitLogger.Error($"Udp packet oversize mtu limit - {bytes.Length}");

                pooledNetworkPacket.Dispose();
                return;
            }

            _udpToolkitLogger.Debug(
                $"Packet sends from: - {_sender.Client.LocalEndPoint} to: {pooledNetworkPacket.Value.IpEndPoint} packet: {pooledNetworkPacket.Value} total bytes length: {bytes.Length}, payload bytes length: {pooledNetworkPacket.Value.Serializer().Length}");

            await _sender
                .SendAsync(bytes, bytes.Length, pooledNetworkPacket.Value.IpEndPoint)
                .ConfigureAwait(false);
        }

        private void SendInternalAsync(
            IConnection connection,
            PooledObject<NetworkPacket> pooledNetworkPacket)
        {
            var networkPacket = pooledNetworkPacket.Value;

            switch (networkPacket.NetworkPacketType)
            {
                case NetworkPacketType.FromServer:
                case NetworkPacketType.FromClient:
                case NetworkPacketType.Protocol:
                    connection
                        .GetOutcomingChannel(channelType: networkPacket.ChannelType)
                        .HandleOutputPacket(pooledNetworkPacket.Value);

                    break;

                case NetworkPacketType.Ack:
                    connection
                        .GetOutcomingChannel(networkPacket.ChannelType)
                        .GetAck(pooledNetworkPacket.Value);

                    break;

                default:
                    throw new NotSupportedException(
                        $"NetworkPacketType {networkPacket.NetworkPacketType} - not supported!");
            }
        }
    }
}
