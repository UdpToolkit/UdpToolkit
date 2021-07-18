namespace UdpToolkit.Core
{
    using System;

    public interface IScheduler : IDisposable
    {
        void Schedule(
            int roomId,
            short timerId,
            TimeSpan dueTime,
            Action action);
    }
}