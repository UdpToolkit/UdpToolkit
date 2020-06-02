namespace UdpToolkit.Framework.Client.Events.EventConsumers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using UdpToolkit.Core;
    using UdpToolkit.Framework.Client.Core;

    public sealed class EventConsumer<TEvent> : IEventConsumer<TEvent>
    {
        private readonly ISerializer _serializer;
        private readonly ConcurrentQueue<TEvent> _queue;

        public EventConsumer(
            ISerializer serializer,
            RpcDescriptorId rpcDescriptorId)
        {
            _serializer = serializer;
            _queue = new ConcurrentQueue<TEvent>();
            RpcDescriptorId = rpcDescriptorId;
        }

        public RpcDescriptorId RpcDescriptorId { get; }

        public IEnumerable<TEvent> Consume()
        {
            return _queue.AsEnumerable();
        }

        public void Enqueue(ArraySegment<byte> payload)
        {
            var @event = _serializer.Deserialize<TEvent>(bytes: payload);

            _queue.Enqueue(item: @event);
        }
    }
}