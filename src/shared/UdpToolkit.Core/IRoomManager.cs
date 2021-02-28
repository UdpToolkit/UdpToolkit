namespace UdpToolkit.Core
{
    using System;
    using System.Collections.Generic;

    public interface IRoomManager
    {
        void JoinOrCreate(
            int roomId,
            Guid connectionId);

        List<Guid> GetRoom(
            int roomId);

        void Leave(
            int roomId,
            Guid connectionId);
    }
}