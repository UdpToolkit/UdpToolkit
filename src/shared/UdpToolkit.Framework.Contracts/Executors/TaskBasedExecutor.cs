namespace UdpToolkit.Framework.Contracts.Executors
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class TaskBasedExecutor : IExecutor
    {
        private readonly TaskFactory _taskFactory;
        private bool _disposed;

        public TaskBasedExecutor()
        {
            _taskFactory = new TaskFactory();
        }

        ~TaskBasedExecutor()
        {
            Dispose(false);
        }

        public event Action<Exception> OnException;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Execute(
            Action action,
            string opName,
            CancellationToken cancellationToken)
        {
            _taskFactory.StartNew(
                action: () =>
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        OnException?.Invoke(ex);
                    }
                },
                cancellationToken: cancellationToken,
                creationOptions: TaskCreationOptions.LongRunning,
                scheduler: TaskScheduler.Current);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // nothing to do
            }

            _disposed = true;
        }
    }
}