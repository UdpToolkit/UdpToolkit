namespace UdpToolkit
{
    using System;
    using System.Collections.Generic;
    using UdpToolkit.Core;
    using UdpToolkit.Core.Executors;
    using UdpToolkit.Logging;
    using UdpToolkit.Network;
    using UdpToolkit.Network.Clients;
    using UdpToolkit.Network.Packets;
    using UdpToolkit.Serialization;

    public sealed class Host : IHost
    {
        private readonly IExecutor _executor;
        private readonly IUdpToolkitLogger _logger;
        private readonly ISerializer _serializer;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IBroadcaster _broadcaster;
        private readonly IHostClient _hostClient;
        private readonly IQueueDispatcher<OutPacket> _hostOutQueueDispatcher;
        private readonly IQueueDispatcher<InPacket> _inQueueDispatcher;
        private readonly IUdpClient[] _udpClients;
        private readonly IList<IDisposable> _toDispose;

        private bool _disposed = false;

        public Host(
            ISerializer serializer,
            ISubscriptionManager subscriptionManager,
            IScheduler scheduler,
            IUdpToolkitLogger logger,
            IBroadcaster broadcaster,
            IHostClient hostClient,
            IQueueDispatcher<OutPacket> hostOutQueueDispatcher,
            IQueueDispatcher<InPacket> inQueueDispatcher,
            IUdpClient[] udpClients,
            IExecutor executor,
            IList<IDisposable> toDispose)
        {
            Scheduler = scheduler;
            _subscriptionManager = subscriptionManager;
            _logger = logger;
            _broadcaster = broadcaster;
            _hostClient = hostClient;
            _hostOutQueueDispatcher = hostOutQueueDispatcher;
            _inQueueDispatcher = inQueueDispatcher;
            _udpClients = udpClients;
            _executor = executor;
            _toDispose = toDispose;
            _serializer = serializer;
        }

        ~Host()
        {
            Dispose(false);
        }

        public IHostClient HostClient => _hostClient;

        public IScheduler Scheduler { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Run()
        {
            for (var i = 0; i < _udpClients.Length; i++)
            {
                _executor.Execute(_udpClients[i].Receive, "Receiver");
            }

            for (var i = 0; i < _inQueueDispatcher.Count; i++)
            {
                _executor.Execute(_inQueueDispatcher[i].Consume, "Worker");
            }

            for (var i = 0; i < _hostOutQueueDispatcher.Count; i++)
            {
                _executor.Execute(_hostOutQueueDispatcher[i].Consume, "Sender");
            }

            _logger.Information($"{nameof(Host)} running...");
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

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                for (var i = 0; i < _toDispose.Count; i++)
                {
                    _toDispose[i].Dispose();
                }
            }

            _logger.Debug($"{this.GetType().Name} - disposed!");
            _disposed = true;
        }
    }
}
