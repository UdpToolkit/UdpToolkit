namespace UdpToolkit.Network.Contracts.Clients
{
    using System;
    using System.Threading;
    using UdpToolkit.Network.Contracts.Connections;
    using UdpToolkit.Network.Contracts.Packets;
    using UdpToolkit.Network.Contracts.Sockets;

    /// <summary>
    /// Custom UDP client with own network protocol.
    /// </summary>
    public interface IUdpClient : IDisposable
    {
        /// <summary>
        /// Raised when new user-defined packet received.
        /// </summary>
        event Action<InNetworkPacket> OnPacketReceived;

        /// <summary>
        /// Raised when outgoing packet dropped.
        /// </summary>
        event Action<InNetworkPacket> OnPacketDropped;

        /// <summary>
        /// Raised when received packet with invalid network header.
        /// </summary>
        event Action<InNetworkPacket> OnInvalidPacketReceived;

        /// <summary>
        /// Raised when user-defined packet expired.
        /// </summary>
        event Action<PendingPacket> OnPacketExpired;

        /// <summary>
        /// Raised when UDP client connected to other UDP client.
        /// </summary>
        /// <remarks>
        /// IpV4Address - source ip
        /// Guid - connectionId
        /// </remarks>
        event Action<IpV4Address, Guid> OnConnected;

        /// <summary>
        /// Raised when UDP client disconnected from other UDP client.
        /// </summary>
        /// <remarks>
        /// IpV4Address - source ip
        /// Guid - connectionId
        /// </remarks>
        event Action<IpV4Address, Guid> OnDisconnected;

        /// <summary>
        /// Raised when ping acknowledge received.
        /// </summary>
        /// <remarks>
        /// Guid - connectionId
        /// double - rtt for specific connection
        /// </remarks>
        event Action<Guid, double> OnPing;

        /// <summary>
        /// Check state of connection to another UDP client.
        /// </summary>
        /// <param name="connectionId">Own connectionId.</param>
        /// <returns>true if connection alive, false - if connection expired or client not connected.</returns>
        bool IsConnected(
            out Guid connectionId);

        /// <summary>
        /// Connect to another UDP client.
        /// </summary>
        /// <param name="ipV4Address">Destination ip address.</param>
        /// <param name="connectionId">Connection identifier.</param>
        /// <param name="routingKey">Routing key.</param>
        void Connect(
            IpV4Address ipV4Address,
            Guid connectionId,
            Guid routingKey);

        /// <summary>
        /// Resend packets.
        /// </summary>
        void ResendPackets();

        /// <summary>
        /// Resend packets.
        /// </summary>
        /// <param name="ipV4Address">Another UDP client address.</param>
        void Ping(
            IpV4Address ipV4Address);

        /// <summary>
        /// Disconnect from another UDP client.
        /// </summary>
        /// <param name="ipV4Address">Another UDP client address.</param>
        void Disconnect(
            IpV4Address ipV4Address);

        /// <summary>
        /// Send user-defined packets.
        /// </summary>
        /// <param name="connectionId">Connection identifier.</param>
        /// <param name="channelId">Channel identifier.</param>
        /// <param name="dataType">Type of data.</param>
        /// <param name="payload">Payload.</param>
        /// <param name="ipV4Address">Destination ip address.</param>
        void Send(
            Guid connectionId,
            byte channelId,
            byte dataType,
            ReadOnlySpan<byte> payload,
            IpV4Address ipV4Address);

        /// <summary>
        /// Start receive packets.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for cancelling receive operation.</param>
        void StartReceive(
            CancellationToken cancellationToken);

        /// <summary>
        /// Start poll packets.
        /// </summary>
        /// <param name="timeout">Polling timeout.</param>
        /// <param name="remoteIp">Remote ip address.</param>
        /// <param name="buffer">Buffer.</param>
        void Poll(
            int timeout,
            ref IpV4Address remoteIp,
            byte[] buffer);

        /// <summary>
        /// Get local ip address of udp client.
        /// </summary>
        /// <returns>Local ip address of udp client.</returns>
        IpV4Address GetLocalIp();

        /// <summary>
        /// Get instance of connection pool.
        /// </summary>
        /// <returns>Shared connection pool.</returns>
        IConnectionPool GetConnectionPool();
    }
}