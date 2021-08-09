namespace UdpToolkit.Framework.Contracts
{
    using System;

    public interface IHost : IDisposable
    {
        IHostClient HostClient { get; }

        void Run();

        void OnCore(
            byte hookId,
            Subscription subscription);

        void SendCore<TEvent>(
            TEvent @event,
            Guid caller,
            int roomId,
            byte hookId,
            byte channelId,
            BroadcastMode broadcastMode);
    }
}