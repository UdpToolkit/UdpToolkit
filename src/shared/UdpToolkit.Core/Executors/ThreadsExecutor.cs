namespace UdpToolkit.Core.Executors
{
    using System;
    using System.Threading;
    using UdpToolkit.Logging;

    public sealed class ThreadsExecutor : IExecutor
    {
        private readonly IUdpToolkitLogger _logger;

        public ThreadsExecutor(IUdpToolkitLogger logger)
        {
            _logger = logger;
        }

        public void Execute(
            Action action,
            bool restartOnFail,
            string opName)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception {ex} on execute action: {opName}");
                    if (restartOnFail)
                    {
                        _logger.Warning($"Restart action {opName}...");
                        Execute(action, true, opName);
                    }
                }
            });

            _logger.Debug($"Run {opName} on thread based executor, threadId - {thread.ManagedThreadId}");

            thread.Start();
        }
    }
}