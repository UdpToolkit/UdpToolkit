namespace UdpToolkit.Framework.Server.Peers
{
    using System;
    using System.Collections.Concurrent;
    using UdpToolkit.Framework.Server.Core;

    public sealed class RoomManager : IRoomManager
    {
        private readonly ConcurrentDictionary<ushort, IRoom> _rooms = new ConcurrentDictionary<ushort, IRoom>();
        private readonly IPeerManager _peerManager;

        public RoomManager(IPeerManager peerManager)
        {
            _peerManager = peerManager;
        }

        public void JoinOrCreate(ushort roomId, Guid peerId)
        {
            var peer = _peerManager.Get(peerId);

            _rooms.AddOrUpdate(
                key: roomId,
                addValueFactory: (id) =>
                {
                    var room = new Room(roomId);
                    room.AddPeer(peer);

                    return room;
                },
                updateValueFactory: (id, room) =>
                {
                    room.AddPeer(peer);
                    return room;
                });
        }

        public void JoinOrCreate(ushort roomId, Guid peerId, int limit)
        {
            var peer = _peerManager.Get(peerId);

            _rooms.AddOrUpdate(
                key: roomId,
                addValueFactory: (id) => new Room(roomId),
                updateValueFactory: (id, room) =>
                {
                    if (room.Size < limit)
                    {
                        room.AddPeer(peer);
                        return room;
                    }

                    return room;
                });
        }

        public IRoom GetRoom(ushort roomId)
        {
            return _rooms[roomId];
        }

        public void Leave(ushort roomId, Guid peerId)
        {
            var peer = _peerManager.Get(peerId: peerId);

            _rooms[roomId].RemovePeer(peerId: peer.PeerId);
        }
    }
}
