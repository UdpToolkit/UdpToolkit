namespace UdpToolkit.Framework
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using UdpToolkit.Core;

    public class PeerManager : IPeerManager, IRawPeerManager
    {
        private readonly ConcurrentDictionary<Guid, Peer> _peers = new ConcurrentDictionary<Guid, Peer>();
        private readonly IDateTimeProvider _dateTimeProvider;

        public PeerManager(
            IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        public void Remove(
            Peer peer)
        {
            _peers.Remove(peer.PeerId, out _);
        }

        public Peer GetPeer(Guid peerId)
        {
            return _peers[peerId];
        }

        IPeer IPeerManager.AddOrUpdate(
            Guid peerId,
            List<IPEndPoint> ips,
            TimeSpan inactivityTimeout)
        {
            return AddOrUpdate(peerId, ips, inactivityTimeout);
        }

        public bool TryGetPeer(Guid peerId, out IPeer peer)
        {
            peer = null;
            var exists = _peers.TryGetValue(peerId, out var rawPeer);
            if (!exists)
            {
                return false;
            }

            peer = rawPeer;

            return true;
        }

        public Peer AddOrUpdate(
            Guid peerId,
            List<IPEndPoint> ips,
            TimeSpan inactivityTimeout)
        {
            return _peers.AddOrUpdate(
                key: peerId,
                addValueFactory: (key) => Peer.New(
                    inactivityTimeout: inactivityTimeout,
                    peerId: peerId,
                    peerIps: ips),
                updateValueFactory: (key, peer) =>
                {
                    peer
                        .OnActivity(lastActivityAt: _dateTimeProvider.UtcNow());

                    foreach (var ip in ips)
                    {
                        if (!peer.PeerIps.Contains(ip))
                        {
                            peer.PeerIps.Add(ip);
                        }
                    }

                    return peer;
                });
        }
    }
}