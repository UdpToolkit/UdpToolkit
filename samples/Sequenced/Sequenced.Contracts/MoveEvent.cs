﻿namespace Sequenced.Contracts
{
    using System;
    using MessagePack;
    using UdpToolkit.Annotations;

    [UdpEvent]
    [MessagePackObject]
    public sealed class MoveEvent : IDisposable
    {
        [Obsolete("Serialization only")]
        public MoveEvent()
        {
        }

        public MoveEvent(
            Guid groupId,
            int id,
            string @from)
        {
            Id = id;
            GroupId = groupId;
            From = @from;
        }

        [Key(0)]
        public Guid GroupId { get; }

        [Key(1)]
        public int Id { get; }

        [Key(2)]
        public string From { get; }

        public void Dispose()
        {
            // nothing to do
        }
    }
}