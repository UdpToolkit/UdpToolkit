namespace UdpToolkit.Core
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    public interface IHost : IDisposable
    {
        IServerHostClient ServerHostClient { get; }

        IScheduler Scheduler { get; }

        Task RunAsync();

        void Stop();

        void OnCore(
            byte hookId,
            Subscription subscription);

        void PublishCore<TEvent>(
            TEvent @event,
            int roomId,
            byte hookId,
            UdpMode udpMode,
            BroadcastMode broadcastMode,
            IPEndPoint ipEndPoint = null);
    }
}
