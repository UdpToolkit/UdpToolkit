namespace UdpToolkit.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UdpToolkit.Core;
    using UdpToolkit.Core.ProtocolEvents;
    using UdpToolkit.Network;
    using UdpToolkit.Network.Channels;
    using UdpToolkit.Network.Pooling;
    using UdpToolkit.Network.Queues;
    using UdpToolkit.Serialization;

    public sealed class ServerHostClient : IServerHostClient
    {
        private readonly Guid _peerId;
        private readonly TimeSpan _connectionTimeoutFromSettings;
        private readonly TimeSpan _resendPacketsTimeout;

        private readonly int? _pingDelayMs;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ISerializer _serializer;
        private readonly IAsyncQueue<PooledObject<CallContext>> _outputQueue;
        private readonly IObjectsPool<CallContext> _callContextPool;
        private readonly List<ClientIp> _clientIps;

        private readonly Task _ping;

        public ServerHostClient(
            Guid peerId,
            ISerializer serializer,
            IDateTimeProvider dateTimeProvider,
            IAsyncQueue<PooledObject<CallContext>> outputQueue,
            TimeSpan connectionTimeout,
            TimeSpan resendPacketsTimeout,
            List<ClientIp> clientIps,
            int? pingDelayMs,
            CancellationTokenSource cancellationTokenSource,
            IObjectsPool<CallContext> callContextPool)
        {
            _serializer = serializer;
            _dateTimeProvider = dateTimeProvider;
            _connectionTimeoutFromSettings = connectionTimeout;
            _resendPacketsTimeout = resendPacketsTimeout;
            _pingDelayMs = pingDelayMs;
            _callContextPool = callContextPool;
            _outputQueue = outputQueue;
            _peerId = peerId;
            _clientIps = clientIps;
            _ping = Task.Run(() => PingHost(cancellationTokenSource.Token));
        }

        public bool IsConnected { get; internal set; }

        public bool Connect(
            TimeSpan? connectionTimeout = null)
        {
            var timeout = connectionTimeout ?? _connectionTimeoutFromSettings;

            PublishInternal(
                resendTimeout: timeout,
                @event: new Connect(_peerId, _clientIps),
                networkPacketType: NetworkPacketType.Protocol,
                broadcastMode: Core.BroadcastMode.Server,
                hookId: (byte)ProtocolHookId.Connect,
                udpMode: UdpMode.ReliableUdp,
                serializer: ProtocolEvent<Connect>.Serialize);

            return SpinWait.SpinUntil(() => IsConnected, TimeSpan.FromMilliseconds(timeout.TotalMilliseconds * 1.2));
        }

        public void ConnectAsync(
            TimeSpan? connectionTimeout = null)
        {
            PublishInternal(
                resendTimeout: connectionTimeout ?? _connectionTimeoutFromSettings,
                @event: new Connect(_peerId, _clientIps),
                networkPacketType: NetworkPacketType.Protocol,
                broadcastMode: Core.BroadcastMode.Server,
                hookId: (byte)ProtocolHookId.Connect,
                udpMode: UdpMode.ReliableUdp,
                serializer: ProtocolEvent<Connect>.Serialize);
        }

        public bool Disconnect()
        {
            PublishInternal(
                resendTimeout: _resendPacketsTimeout,
                @event: new Disconnect(peerId: _peerId),
                broadcastMode: Core.BroadcastMode.Server,
                networkPacketType: NetworkPacketType.Protocol,
                hookId: (byte)ProtocolHookId.Disconnect,
                udpMode: UdpMode.ReliableUdp,
                serializer: ProtocolEvent<Disconnect>.Serialize);

            return SpinWait.SpinUntil(() => !IsConnected, TimeSpan.FromMilliseconds(_resendPacketsTimeout.TotalMilliseconds * 1.2));
        }

        public void Publish<TEvent>(
            TEvent @event,
            byte hookId,
            UdpMode udpMode)
        {
            PublishInternal(
                networkPacketType: NetworkPacketType.FromClient,
                broadcastMode: Core.BroadcastMode.Server,
                resendTimeout: _resendPacketsTimeout,
                @event: @event,
                hookId: hookId,
                udpMode: udpMode,
                serializer: _serializer.Serialize);
        }

        private void PublishInternal<TEvent>(
            TEvent @event,
            TimeSpan resendTimeout,
            byte hookId,
            UdpMode udpMode,
            NetworkPacketType networkPacketType,
            Core.BroadcastMode broadcastMode,
            Func<TEvent, byte[]> serializer)
        {
            var pooledCallContext = _callContextPool.Get();

            pooledCallContext.Value.Set(
                roomId: null,
                broadcastMode: broadcastMode,
                resendTimeout: resendTimeout,
                createdAt: _dateTimeProvider.UtcNow());

            pooledCallContext.Value.NetworkPacketDto.Set(
                id: default,
                acks: default,
                createdAt: _dateTimeProvider.UtcNow(),
                networkPacketType: networkPacketType,
                channelType: udpMode.Map(),
                peerId: _peerId,
                ipEndPoint: null,
                serializer: () => serializer(@event),
                hookId: hookId);

            _outputQueue.Produce(pooledCallContext);
        }

        private async Task PingHost(
            CancellationToken token)
        {
            if (!_pingDelayMs.HasValue)
            {
                return;
            }

            var pingDelay = _pingDelayMs.Value;
            while (!token.IsCancellationRequested)
            {
                if (IsConnected)
                {
                    var @event = new Ping();

                    PublishInternal(
                        resendTimeout: _resendPacketsTimeout,
                        @event: @event,
                        networkPacketType: NetworkPacketType.Protocol,
                        broadcastMode: Core.BroadcastMode.Server,
                        hookId: (byte)ProtocolHookId.Ping,
                        udpMode: UdpMode.ReliableUdp,
                        serializer: ProtocolEvent<Ping>.Serialize);
                }

                await Task.Delay(pingDelay, token).ConfigureAwait(false);
            }
        }
    }
}
