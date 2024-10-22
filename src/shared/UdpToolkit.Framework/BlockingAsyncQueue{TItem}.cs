namespace UdpToolkit.Framework
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;
    using UdpToolkit.Framework.Contracts;
    using UdpToolkit.Framework.Contracts.Events;

    /// <summary>
    /// Async queue based on .NET BlockingCollection.
    /// </summary>
    /// <typeparam name="TItem">
    /// Type of item in the async queue.
    /// </typeparam>
    public sealed class BlockingAsyncQueue<TItem> : IAsyncQueue<TItem>
    {
        private readonly string _id;
        private readonly Action<TItem> _action;
        private readonly BlockingCollection<TItem> _input;
        private readonly IHostEventReporter _hostEventReporter;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockingAsyncQueue{TItem}"/> class.
        /// </summary>
        /// <param name="boundedCapacity">Size of capacity for the async queue.</param>
        /// <param name="action">Action for process consumed items.</param>
        /// <param name="hostEventReporter">Host event reporter.</param>
        /// <param name="id">Queue identifier.</param>
        public BlockingAsyncQueue(
            int boundedCapacity,
            Action<TItem> action,
            IHostEventReporter hostEventReporter,
            string id)
        {
            _action = action;
            _hostEventReporter = hostEventReporter;
            _id = id;
            _input = new BlockingCollection<TItem>(
                boundedCapacity: boundedCapacity,
                collection: new ConcurrentQueue<TItem>());
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="BlockingAsyncQueue{TItem}"/> class.
        /// </summary>
        [ExcludeFromCodeCoverage]
        ~BlockingAsyncQueue()
        {
            Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Produces items to the async queue.
        /// </summary>
        /// <param name="item">Produced item.</param>
        public void Produce(
            TItem item)
        {
            try
            {
                _input.Add(item);
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
        }

        /// <summary>
        /// Consumes items in the async queue.
        /// </summary>
        public void Consume()
        {
            try
            {
                foreach (var item in _input.GetConsumingEnumerable())
                {
                    try
                    {
                        _hostEventReporter.Handle(new QueueItemConsumed(_id));
                        _action(item);
                    }
                    catch (Exception ex)
                    {
                        _hostEventReporter.Handle(new ExceptionThrown(ex));
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _input.CompleteAdding();
                _input.Dispose();
            }

            _disposed = true;
        }
    }
}