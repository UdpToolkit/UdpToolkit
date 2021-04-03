namespace UdpToolkit
{
    using System;
    using System.Collections.Generic;
    using UdpToolkit.Contexts;
    using UdpToolkit.Core;
    using UdpToolkit.Core.Executors;
    using UdpToolkit.Logging;
    using UdpToolkit.Network;
    using UdpToolkit.Network.Clients;
    using UdpToolkit.Serialization;

    public sealed class Host : IHost
    {
        private readonly IExecutor _executor;
        private readonly IUdpToolkitLogger _udpToolkitLogger;
        private readonly ISerializer _serializer;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IBroadcaster _broadcaster;
        private readonly IHostClient _hostClient;
        private readonly IQueueDispatcher<HostOutContext> _hostOutQueueDispatcher;
        private readonly IQueueDispatcher<InContext> _inQueueDispatcher;
        private readonly IEnumerable<IUdpReceiver> _receivers;

        public Host(
            ISerializer serializer,
            ISubscriptionManager subscriptionManager,
            IScheduler scheduler,
            IUdpToolkitLogger udpToolkitLogger,
            IBroadcaster broadcaster,
            IHostClient hostClient,
            IQueueDispatcher<HostOutContext> hostOutQueueDispatcher,
            IQueueDispatcher<InContext> inQueueDispatcher,
            IEnumerable<IUdpReceiver> receivers,
            IExecutor executor)
        {
            Scheduler = scheduler;
            _subscriptionManager = subscriptionManager;
            _udpToolkitLogger = udpToolkitLogger;
            _broadcaster = broadcaster;
            _hostClient = hostClient;
            _hostOutQueueDispatcher = hostOutQueueDispatcher;
            _inQueueDispatcher = inQueueDispatcher;
            _receivers = receivers;
            _executor = executor;
            _serializer = serializer;
        }

        public IHostClient HostClient => _hostClient;

        public IScheduler Scheduler { get; }

        public void Run()
        {
            _hostOutQueueDispatcher.RunAll();
            _inQueueDispatcher.RunAll();

            foreach (var udpReceiver in _receivers)
            {
                _executor.Execute(
                    action: udpReceiver.Receive,
                    restartOnFail: true,
                    opName: "Receive packets");
            }

            _udpToolkitLogger.Information($"{nameof(Host)} running...");
        }

        public void Stop()
        {
            _hostOutQueueDispatcher.StopAll();
            _inQueueDispatcher.StopAll();
        }

        public void OnCore(
            byte hookId,
            Subscription subscription)
        {
            _subscriptionManager.Subscribe(hookId, subscription);
        }

        public void SendCore<TEvent>(
            TEvent @event,
            Guid caller,
            int roomId,
            byte hookId,
            UdpMode udpMode,
            BroadcastMode broadcastMode)
        {
            _broadcaster.Broadcast(
                serializer: () => _serializer.Serialize(@event),
                packetType: PacketType.FromServer,
                caller: caller,
                roomId: roomId,
                hookId: hookId,
                channelType: udpMode.Map(),
                broadcastMode: broadcastMode);
        }

        public void Dispose()
        {
            _hostOutQueueDispatcher.Dispose();
            _inQueueDispatcher.Dispose();
            _broadcaster.Dispose();
        }
    }
}
