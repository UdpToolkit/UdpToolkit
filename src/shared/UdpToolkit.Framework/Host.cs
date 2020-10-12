namespace UdpToolkit.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Serilog;
    using UdpToolkit.Core;
    using UdpToolkit.Network.Channels;
    using UdpToolkit.Network.Clients;
    using UdpToolkit.Network.Packets;
    using UdpToolkit.Network.Queues;
    using UdpToolkit.Serialization;

    public sealed class Host : IHost
    {
        private readonly ILogger _logger = Log.ForContext<Host>();

        private readonly HostSettings _hostSettings;

        private readonly IAsyncQueue<NetworkPacket> _outputQueue;
        private readonly IAsyncQueue<NetworkPacket> _inputQueue;

        private readonly IEnumerable<IUdpSender> _senders;
        private readonly IEnumerable<IUdpReceiver> _receivers;

        private readonly IRawPeerManager _rawPeerManager;

        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IProtocolSubscriptionManager _protocolSubscriptionManager;

        private readonly IRoomManager _roomManager;
        private readonly ServerHostClient _serverHostClient;
        private readonly IBroadcastStrategyResolver _broadcastStrategyResolver;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IScheduler _scheduler;
        private readonly IServerSelector _serverSelector;

        public Host(
            HostSettings hostSettings,
            IAsyncQueue<NetworkPacket> outputQueue,
            IAsyncQueue<NetworkPacket> inputQueue,
            IEnumerable<IUdpSender> senders,
            IEnumerable<IUdpReceiver> receivers,
            ISubscriptionManager subscriptionManager,
            IRawPeerManager rawPeerManager,
            IRoomManager roomManager,
            IProtocolSubscriptionManager protocolSubscriptionManager,
            ServerHostClient serverHostClient,
            IBroadcastStrategyResolver broadcastStrategyResolver,
            IDateTimeProvider dateTimeProvider,
            IScheduler scheduler,
            IServerSelector serverSelector)
        {
            _hostSettings = hostSettings;
            _outputQueue = outputQueue;
            _senders = senders;
            _receivers = receivers;
            _subscriptionManager = subscriptionManager;
            _serverHostClient = serverHostClient;
            _broadcastStrategyResolver = broadcastStrategyResolver;
            _dateTimeProvider = dateTimeProvider;
            _scheduler = scheduler;
            _serverSelector = serverSelector;
            _rawPeerManager = rawPeerManager;
            _roomManager = roomManager;
            _protocolSubscriptionManager = protocolSubscriptionManager;
            _inputQueue = inputQueue;

            foreach (var receiver in _receivers)
            {
                receiver.UdpPacketReceived += _inputQueue.Produce;
            }
        }

        public IServerHostClient ServerHostClient => _serverHostClient;

        public async Task RunAsync()
        {
            var senders = _senders
                .Select(
                    sender => TaskUtils.RunWithRestartOnFail(
                        job: () => StartSenderAsync(sender),
                        logger: (exception) =>
                        {
                            _logger.Error("Exception on send task: {@Exception}", exception);
                            _logger.Warning("Restart sender...");
                        },
                        token: default))
                .ToList();

            var receivers = _receivers
                .Select(
                    receiver => TaskUtils.RunWithRestartOnFail(
                        job: () => StartReceiverAsync(receiver),
                        logger: (exception) =>
                        {
                            _logger.Error("Exception on receive task: {@Exception}", exception);
                            _logger.Warning("Restart receiver...");
                        },
                        token: default))
                .ToList();

            var workers = Enumerable
                .Range(0, _hostSettings.Workers)
                .Select(_ => Task.Run(StartWorkerAsync))
                .ToList();

            var tasks = senders
                .Concat(receivers)
                .Concat(workers);

            _logger.Information($"{nameof(Host)} running...");

            await Task
                .WhenAll(tasks)
                .ConfigureAwait(false);
        }

        public void Stop()
        {
            _inputQueue.Stop();
            _outputQueue.Stop();
        }

        public void OnCore(Subscription subscription, byte hookId)
        {
            _subscriptionManager.Subscribe(hookId, subscription);
        }

        public void Publish<TEvent>(
            TEvent @event,
            ushort roomId,
            byte hookId,
            UdpMode udpMode)
        {
            _broadcastStrategyResolver
                .Resolve(
                    broadcastType: BroadcastType.Room)
                .Execute(
                    roomId: roomId,
                    networkPacket: new NetworkPacket(
                        networkPacketType: NetworkPacketType.UserDefined,
                        createdAt: _dateTimeProvider.UtcNow(),
                        resendTimeout: _hostSettings.ResendPacketsTimeout,
                        channelType: udpMode.Map(),
                        peerId: default,
                        channelHeader: default,
                        serializer: () => _hostSettings.Serializer.Serialize(@event),
                        ipEndPoint: null,
                        hookId: hookId));
        }

        public void Dispose()
        {
            foreach (var sender in _senders)
            {
                sender.Dispose();
            }

            foreach (var receiver in _receivers)
            {
                receiver.Dispose();
            }

            _inputQueue.Dispose();
            _outputQueue.Dispose();
        }

        private void StartWorkerAsync()
        {
            foreach (var networkPacket in _inputQueue.Consume())
            {
                try
                {
                    switch (networkPacket.NetworkPacketType)
                    {
                        case NetworkPacketType.UserDefined:
                            ProcessUserDefinedInputEvent(networkPacket: networkPacket);
                            break;
                        case NetworkPacketType.Protocol:
                            ProcessProtocolInputEvent(networkPacket: networkPacket);
                            break;
                        case NetworkPacketType.Ack:
                            if (networkPacket.IsProtocolEvent)
                            {
                                ProcessInputProtocolAck(networkPacket: networkPacket);
                            }
                            else
                            {
                                ProcessInputAck(networkPacket: networkPacket);
                            }

                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning("Unhandled exception on process packet, {@Exception}", ex);
                }
            }
        }

        private async Task StartReceiverAsync(
            IUdpReceiver udpReceiver)
        {
            await udpReceiver
                .StartReceiveAsync()
                .ConfigureAwait(false);
        }

        private async Task StartSenderAsync(
            IUdpSender udpSender)
        {
            foreach (var networkPacket in _outputQueue.Consume())
            {
                switch (networkPacket.NetworkPacketType)
                {
                    case NetworkPacketType.UserDefined:
                        break;
                    case NetworkPacketType.Protocol:
                        ProcessProtocolOutputEvent(networkPacket: networkPacket);
                        break;
                    case NetworkPacketType.Ack:
                        break;
                }

                await udpSender
                    .SendAsync(networkPacket)
                    .ConfigureAwait(false);
            }
        }

        private void ProcessProtocolInputEvent(
            NetworkPacket networkPacket)
        {
            var protocolHookId = networkPacket.ProtocolHookId;
            var protocolSubscription = _protocolSubscriptionManager
                .GetProtocolSubscription((byte)protocolHookId);

            var userDefinedSubscription = _subscriptionManager
                .GetSubscription((byte)protocolHookId);

            var peer = _rawPeerManager.AddOrUpdate(
                inactivityTimeout: _hostSettings.PeerInactivityTimeout,
                peerId: networkPacket.PeerId,
                ips: new List<IPEndPoint>());

            var inputHandled = peer
                .GetChannel(networkPacket.ChannelType)
                .HandleInputPacket(networkPacket);

            if (!inputHandled)
            {
                // resend lost ack
                return;
            }

            switch (protocolHookId)
            {
                case ProtocolHookId.Ping:
                case ProtocolHookId.Pong:
                case ProtocolHookId.P2P:
                case ProtocolHookId.Disconnect:
                case ProtocolHookId.Connect:
                    protocolSubscription?.OnInputEvent(
                        arg1: networkPacket.Serializer(),
                        arg2: networkPacket.PeerId);

                    userDefinedSubscription?.OnEvent(
                        networkPacket.Serializer(),
                        networkPacket.PeerId,
                        _hostSettings.Serializer,
                        _roomManager,
                        _scheduler);

                    _broadcastStrategyResolver
                        .Resolve(
                            broadcastType: protocolSubscription?.BroadcastMode.Map() ?? BroadcastType.Caller)
                        .Execute(
                            roomId: ushort.MaxValue,
                            networkPacket: networkPacket);

                    break;
            }
        }

        private void ProcessUserDefinedInputEvent(
            NetworkPacket networkPacket)
        {
            var rawPeer = _rawPeerManager
                .GetPeer(peerId: networkPacket.PeerId);

            var channel = rawPeer
                .GetChannel(channelType: networkPacket.ChannelType);

            var userDefinedSubscription = _subscriptionManager
                .GetSubscription(networkPacket.HookId);

            var packetHandled = channel
                .HandleInputPacket(networkPacket: networkPacket);

            if (!packetHandled)
            {
                _logger.Debug($"Input NetworkPacket dropped! {networkPacket.ChannelHeader.Id}");

                return;
            }

            userDefinedSubscription?.OnEvent(
                networkPacket.Serializer(),
                networkPacket.PeerId,
                _hostSettings.Serializer,
                _roomManager,
                _scheduler);

            var result = _serverSelector.IsServerIp(networkPacket.IpEndPoint);

            var ip = result
                ? _serverSelector
                    .GetServer()
                    .GetRandomIp()
                : rawPeer.GetRandomIp();

            var ackPacket = channel
                .GetAck(networkPacket, ip);

            _outputQueue.Produce(ackPacket);
        }

        private void ProcessInputAck(
            NetworkPacket networkPacket)
        {
            var rawPeer = _rawPeerManager
                .GetPeer(peerId: networkPacket.PeerId);

            var channel = rawPeer
                .GetChannel(channelType: networkPacket.ChannelType);

            var userDefinedSubscription = _subscriptionManager
                .GetSubscription(networkPacket.HookId);

            var ackHandled = channel
                .HandleAck(networkPacket: networkPacket);

            if (!ackHandled)
            {
                _logger.Debug($"Ack NetworkPacket dropped! {networkPacket.ChannelHeader.Id}");

                return;
            }

            userDefinedSubscription?
                .OnAck(networkPacket.PeerId);
        }

        private void ProcessInputProtocolAck(
            NetworkPacket networkPacket)
        {
            var protocolHookId = networkPacket.ProtocolHookId;
            var userDefinedSubscription = _subscriptionManager
                .GetSubscription((byte)protocolHookId);

            var protocolSubscription = _protocolSubscriptionManager
                .GetProtocolSubscription((byte)protocolHookId);

            var peer = _rawPeerManager.AddOrUpdate(
                inactivityTimeout: _hostSettings.PeerInactivityTimeout,
                peerId: networkPacket.PeerId,
                ips: new List<IPEndPoint>());

            var ackHandled = peer
                .GetChannel(networkPacket.ChannelType)
                .HandleAck(networkPacket);

            if (!ackHandled)
            {
                _logger.Debug("Ack dropped - {@packet}", networkPacket);
                return;
            }

            switch (protocolHookId)
            {
                case ProtocolHookId.Ping:
                case ProtocolHookId.Pong:
                case ProtocolHookId.P2P:
                case ProtocolHookId.Disconnect:
                case ProtocolHookId.Connect:
                    switch (protocolHookId)
                    {
                        case ProtocolHookId.Connect:
                        case ProtocolHookId.Disconnect:
                            _serverHostClient.IsConnected = protocolHookId == ProtocolHookId.Connect;
                            break;
                    }

                    protocolSubscription?
                        .OnAck(peer.PeerId);

                    userDefinedSubscription?
                        .OnAck(networkPacket.PeerId);
                    break;
            }
        }

        private void ProcessProtocolOutputEvent(
            NetworkPacket networkPacket)
        {
            var protocolHookId = networkPacket.ProtocolHookId;
            var protocolSubscription = _protocolSubscriptionManager
                .GetProtocolSubscription((byte)networkPacket.ProtocolHookId);

            switch (protocolHookId)
            {
                case ProtocolHookId.Disconnect when _serverHostClient.IsConnected:
                case ProtocolHookId.P2P when _serverHostClient.IsConnected:
                case ProtocolHookId.Ping when _serverHostClient.IsConnected:
                case ProtocolHookId.Pong when _serverHostClient.IsConnected:
                case ProtocolHookId.Connect:
                    protocolSubscription?.OnOutputEvent(
                        arg1: networkPacket.Serializer(),
                        arg2: networkPacket.PeerId);
                    break;
            }
        }
    }
}
