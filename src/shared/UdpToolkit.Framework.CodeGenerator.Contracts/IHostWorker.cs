// ReSharper disable once CheckNamespace
namespace UdpToolkit.Framework.Contracts
{
    using System;
    using UdpToolkit.Logging;
    using UdpToolkit.Network.Contracts.Clients;
    using UdpToolkit.Serialization;

    /// <summary>
    /// Abstraction for processing input and output host packets.
    /// </summary>
    public interface IHostWorker : IDisposable
    {
        /// <summary>
        /// Gets or sets instance of logger.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets instance of serializer.
        /// </summary>
        ISerializer Serializer { get; set; }

        /// <summary>
        /// Gets or sets instance of broadcaster.
        /// </summary>
        IBroadcaster Broadcaster { get; set; }

        /// <summary>
        /// Process input packet.
        /// </summary>
        /// <param name="inPacket">Input packet.</param>
        void Process(
            NetworkPacket inPacket);

        /// <summary>
        /// Process output packet.
        /// </summary>
        /// <param name="outPacket">Output packet.</param>
        /// <param name="payload">Payload in bytes representation.</param>
        /// <param name="subscriptionId">Subscription identifier.</param>
        /// <returns>
        /// An array of bytes for sending over the network.
        /// </returns>
        bool Process(
            OutPacket outPacket,
            out ReadOnlySpan<byte> payload,
            out byte subscriptionId);
    }
}