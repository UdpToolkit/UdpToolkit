namespace UdpToolkit
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using UdpToolkit.Core;
    using UdpToolkit.Jobs;
    using UdpToolkit.Network;
    using UdpToolkit.Network.Clients;
    using UdpToolkit.Network.Packets;
    using UdpToolkit.Network.Pooling;
    using UdpToolkit.Network.Queues;

    public sealed class HostBuilder : IHostBuilder
    {
        private readonly HostSettings _hostSettings;
        private readonly HostClientSettings _hostClientSettings;

        public HostBuilder(
            HostSettings hostSettings,
            HostClientSettings hostClientSettings)
        {
            _hostSettings = hostSettings;
            _hostClientSettings = hostClientSettings;
        }

        public IHostBuilder ConfigureHost(Action<HostSettings> configurator)
        {
            configurator(_hostSettings);

            return this;
        }

        public IHostBuilder ConfigureHostClient(Action<HostClientSettings> configurator)
        {
            configurator(_hostClientSettings);

            return this;
        }

        public IHost Build()
        {
            ValidateSettings(_hostSettings);

            var dateTimeProvider = new DateTimeProvider();
            var udpClientFactory = new UdpClientFactory();
            var scheduler = new Scheduler();
            var loggerFactory = _hostSettings.LoggerFactory;

            var callContextPool = new ObjectsPool<CallContext>(CallContext.Create, 1000);
            var networkPacketPool = new ObjectsPool<NetworkPacket>(NetworkPacket.Create, 1000);

            var outputQueue = new BlockingAsyncQueue<PooledObject<CallContext>>(
                boundedCapacity: int.MaxValue);

            var inputQueue = new BlockingAsyncQueue<PooledObject<CallContext>>(
                boundedCapacity: int.MaxValue);

            var resendQueue = new BlockingAsyncQueue<PooledObject<CallContext>>(
                boundedCapacity: int.MaxValue);

            var peerManager = new PeerManager(
                dateTimeProvider: dateTimeProvider);

            var roomManager = new RoomManager(
                peerManager: peerManager);

            if (!_hostClientSettings.ServerInputPorts.Any())
            {
                _hostClientSettings.ServerInputPorts = new int[]
                {
                    NetworkUtils.GetAvailablePort(),
                    NetworkUtils.GetAvailablePort(),
                };
            }

            var inputIps = _hostClientSettings.ServerInputPorts
                .Select(port =>
                    new IPEndPoint(
                        address: IPAddress.Parse(ipString: _hostClientSettings.ServerHost),
                        port: port))
                .ToArray();

            var randomServerSelector = new RandomServerSelector(
                peerManager: peerManager,
                inputIps: inputIps);

            if (!_hostSettings.OutputPorts.Any())
            {
                _hostSettings.OutputPorts = new[]
                {
                    NetworkUtils.GetAvailablePort(),
                    NetworkUtils.GetAvailablePort(),
                };
            }

            var outputPorts = _hostSettings.OutputPorts
                .Select(port => new IPEndPoint(IPAddress.Parse(_hostSettings.Host), port))
                .ToList();

            var senders = outputPorts
                .Select(udpClientFactory.Create)
                .Select(udpClient => new UdpSender(
                    udpToolkitLogger: loggerFactory.Create<UdpSender>(),
                    sender: udpClient,
                    rawRoomManager: roomManager,
                    networkPacketPool: networkPacketPool))
                .ToList();

            if (!_hostSettings.InputPorts.Any())
            {
                _hostSettings.InputPorts = new[]
                {
                    NetworkUtils.GetAvailablePort(),
                    NetworkUtils.GetAvailablePort(),
                };
            }

            var inputPorts = _hostSettings.InputPorts
                .Select(port => new IPEndPoint(IPAddress.Parse(_hostSettings.Host), port))
                .ToList();

            var receivers = inputPorts
                .Select(udpClientFactory.Create)
                .Select(udpClient => new UdpReceiver(
                    udpToolkitLogger: loggerFactory.Create<UdpReceiver>(),
                    peerInactivityTimeout: _hostSettings.PeerInactivityTimeout,
                    rawPeerManager: peerManager,
                    receiver: udpClient,
                    networkPacketPool: networkPacketPool))
                .ToList();

            var subscriptionManager = new SubscriptionManager();

            var protocolSubscriptionManager = new ProtocolSubscriptionManager();
            var protocolSubscriptionBootstraper = new ProtocolSubscriptionBootstraper(
                protocolSubscriptionManager: protocolSubscriptionManager,
                udpToolkitLogger: loggerFactory.Create<ProtocolSubscriptionBootstraper>(),
                peerManager: peerManager,
                dateTimeProvider: dateTimeProvider,
                inactivityTimeout: _hostSettings.PeerInactivityTimeout);
            protocolSubscriptionBootstraper.BootstrapSubscriptions();

            var peerId = Guid.NewGuid();

            var hostClient = new HostClient(
                inputPorts: _hostSettings.InputPorts.ToArray(),
                callContextPool: callContextPool,
                peerId: peerId,
                cancellationTokenSource: new CancellationTokenSource(),
                outputQueue: outputQueue,
                pingDelayMs: _hostClientSettings.PingDelayInMs,
                resendPacketsTimeout: _hostClientSettings.ResendPacketsTimeout,
                connectionTimeout: _hostClientSettings.ConnectionTimeout,
                dateTimeProvider: dateTimeProvider,
                serializer: _hostSettings.Serializer);

            _ = peerManager.AddOrUpdate(
                inactivityTimeout: _hostSettings.PeerInactivityTimeout,
                peerId: peerId,
                ips: inputPorts);

            return new Host(
                udpToolkitLogger: loggerFactory.Create<Host>(),
                callContextPool: callContextPool,
                dateTimeProvider: dateTimeProvider,
                hostSettings: _hostSettings,
                subscriptionManager: subscriptionManager,
                outputQueue: outputQueue,
                inputQueue: inputQueue,
                senders: senders,
                receivers: receivers,
                scheduler: scheduler,
                sendingJob: new SenderJob(
                    udpToolkitLogger: loggerFactory.Create<SenderJob>(),
                    scheduler: scheduler,
                    resendQueue: new ResendQueue(),
                    rawPeerManager: peerManager,
                    serverSelector: randomServerSelector,
                    networkPacketPool: networkPacketPool,
                    protocolSubscriptionManager: protocolSubscriptionManager,
                    outputQueue: outputQueue),
                receivingJob: new ReceiverJob(
                    hostSettings: _hostSettings,
                    dateTimeProvider: dateTimeProvider,
                    callContextPool: callContextPool,
                    inputQueue: inputQueue),
                workerJob: new WorkerJob(
                    udpToolkitLogger: loggerFactory.Create<WorkerJob>(),
                    inputQueue: inputQueue,
                    outputQueue: outputQueue,
                    callContextPool: callContextPool,
                    subscriptionManager: subscriptionManager,
                    protocolSubscriptionManager: protocolSubscriptionManager,
                    roomManager: roomManager,
                    dateTimeProvider: dateTimeProvider,
                    hostSettings: _hostSettings,
                    scheduler: scheduler,
                    hostClient: hostClient));
        }

        private void ValidateSettings(
            HostSettings hostSettings)
        {
            if (hostSettings.Serializer == null)
            {
                throw new ArgumentNullException(nameof(hostSettings.Serializer), "PLease install serializer NuGet Package, or write your own..");
            }

            if (hostSettings.LoggerFactory == null)
            {
                throw new ArgumentNullException(nameof(hostSettings.Serializer), "PLease install logging NuGet Package, or write your own..");
            }
        }
    }
}