namespace UdpToolkit.Framework.Contracts
{
    using System;
    using UdpToolkit.Network.Contracts.Sockets;

    /// <summary>
    /// Information about connection in the group.
    /// </summary>
    public readonly struct GroupConnection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupConnection"/> struct.
        /// </summary>
        /// <param name="connectionId">Connection identifier.</param>
        /// <param name="ipV4Address">Ip address of connection.</param>
        public GroupConnection(
            Guid connectionId,
            IpV4Address ipV4Address)
        {
            ConnectionId = connectionId;
            IpV4Address = ipV4Address;
        }

        /// <summary>
        /// Gets connection identifier.
        /// </summary>
        public Guid ConnectionId { get; }

        /// <summary>
        /// Gets ip address of connection.
        /// </summary>
        public IpV4Address IpV4Address { get; }
    }
}