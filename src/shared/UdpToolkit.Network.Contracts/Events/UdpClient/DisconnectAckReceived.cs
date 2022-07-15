namespace UdpToolkit.Network.Contracts.Events.UdpClient
{
    using UdpToolkit.Network.Contracts.Sockets;

    /// <summary>
    /// Raised when received disconnect ack packet.
    /// </summary>
    public readonly struct DisconnectAckReceived
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectAckReceived"/> struct.
        /// </summary>
        /// <param name="remoteIp">Remote ip address.</param>
        public DisconnectAckReceived(IpV4Address remoteIp)
        {
            RemoteIp = remoteIp;
        }

        /// <summary>
        /// Gets remote ip address.
        /// </summary>
        public IpV4Address RemoteIp { get; }
    }
}