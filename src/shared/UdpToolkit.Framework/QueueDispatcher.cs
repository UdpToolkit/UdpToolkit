namespace UdpToolkit.Framework
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using UdpToolkit.Framework.Contracts;

    /// <inheritdoc />
    public sealed class QueueDispatcher<TEvent> : IQueueDispatcher<TEvent>
    {
        private readonly IAsyncQueue<TEvent>[] _queues;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueDispatcher{TEvent}"/> class.
        /// </summary>
        /// <param name="queues">Array of async queues.</param>
        public QueueDispatcher(
            IAsyncQueue<TEvent>[] queues)
        {
            _queues = queues;
            Count = _queues.Length;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="QueueDispatcher{TEvent}"/> class.
        /// </summary>
        [ExcludeFromCodeCoverage]
        ~QueueDispatcher()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public int Count { get; }

        /// <inheritdoc />
        public IAsyncQueue<TEvent> this[int index]
        {
            get => _queues[index];
            set => _queues[index] = value;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public IAsyncQueue<TEvent> Dispatch(Guid connectionId)
        {
            return _queues[MurMurHash.Hash3_x86_32(connectionId) % _queues.Length];
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                for (int i = 0; i < _queues.Length; i++)
                {
                    _queues[i].Dispose();
                }
            }

            _disposed = true;
        }
    }
}