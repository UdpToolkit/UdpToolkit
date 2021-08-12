namespace UdpToolkit.Framework
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using UdpToolkit.Framework.Contracts;
    using UdpToolkit.Logging;
    using UdpToolkit.Network.Contracts.Channels;
    using UdpToolkit.Network.Contracts.Clients;
    using UdpToolkit.Network.Contracts.Packets;
    using UdpToolkit.Network.Contracts.Protocol;
    using UdpToolkit.Network.Contracts.Sockets;
    using UdpToolkit.Serialization;

    public sealed class HostClient : IHostClient
    {
        private readonly TaskFactory _taskFactory;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly HostClientSettingsInternal _hostClientSettingsInternal;

        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUdpClientFactory _udpClientFactory;
        private readonly ISerializer _serializer;
        private readonly IUdpToolkitLogger _logger;
        private readonly IQueueDispatcher<OutPacket> _outQueueDispatcher;

        private bool _disposed = false;

        private DateTimeOffset _startConnect;

        public HostClient(
            IUdpClientFactory udpClientFactory,
            ISerializer serializer,
            HostClientSettingsInternal hostClientSettingsInternal,
            IDateTimeProvider dateTimeProvider,
            IUdpToolkitLogger logger,
            TaskFactory taskFactory,
            CancellationTokenSource cancellationTokenSource,
            IQueueDispatcher<OutPacket> outQueueDispatcher)
        {
            _serializer = serializer;
            _hostClientSettingsInternal = hostClientSettingsInternal;
            _taskFactory = taskFactory;
            _cancellationTokenSource = cancellationTokenSource;
            _outQueueDispatcher = outQueueDispatcher;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
            _taskFactory = taskFactory;
            _udpClientFactory = udpClientFactory;
        }

        ~HostClient()
        {
            Dispose(false);
        }

        public event Action OnConnectionTimeout;

        public Guid ConnectionId => _hostClientSettingsInternal.ConnectionId;

        public bool IsConnected { get; internal set; }

        public TimeSpan? Rtt => _udpClientFactory.GetRtt(ConnectionId);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Connect()
        {
            _startConnect = _dateTimeProvider.UtcNow();

            SendInternal(
                @event: new Connect(ConnectionId),
                packetType: PacketType.Protocol,
                hookId: (byte)ProtocolHookId.Connect,
                destination: _hostClientSettingsInternal.ServerIpV4,
                channelId: ReliableChannelConsts.ReliableChannel,
                serializer: ProtocolEvent<Connect>.Serialize);

            var token = _cancellationTokenSource.Token;

            _taskFactory.StartNew(
                function: () => StartHeartbeat(token),
                cancellationToken: token,
                creationOptions: TaskCreationOptions.LongRunning,
                scheduler: TaskScheduler.Current);
        }

        public void ConnectToPeer(
            string host,
            int port)
        {
            SendInternal(
                @event: new ConnectToPeer(ConnectionId),
                packetType: PacketType.Protocol,
                hookId: (byte)ProtocolHookId.Connect2Peer,
                destination: new IpV4Address
                {
                    Address = IPAddress.Parse(host).ToInt(),
                    Port = (ushort)port,
                },
                channelId: ReliableChannelConsts.ReliableChannel,
                serializer: ProtocolEvent<ConnectToPeer>.Serialize);
        }

        public void Disconnect()
        {
            SendInternal(
                @event: new Disconnect(connectionId: ConnectionId),
                packetType: PacketType.Protocol,
                hookId: (byte)ProtocolHookId.Disconnect,
                destination: _hostClientSettingsInternal.ServerIpV4,
                channelId: ReliableChannelConsts.ReliableChannel,
                serializer: ProtocolEvent<Disconnect>.Serialize);
        }

        public void Send<TEvent>(
            TEvent @event,
            byte hookId,
            IpV4Address destination,
            byte channelId)
        {
            SendInternal(
                packetType: PacketType.FromClient,
                @event: @event,
                hookId: hookId,
                destination: destination,
                channelId: channelId,
                serializer: _serializer.Serialize);
        }

        public void Send<TEvent>(
            TEvent @event,
            byte hookId,
            byte channelId)
        {
            SendInternal(
                packetType: PacketType.FromClient,
                @event: @event,
                hookId: hookId,
                destination: _hostClientSettingsInternal.ServerIpV4,
                channelId: channelId,
                serializer: _serializer.Serialize);
        }

        private void SendInternal<TEvent>(
            TEvent @event,
            byte hookId,
            IpV4Address destination,
            byte channelId,
            PacketType packetType,
            Func<TEvent, byte[]> serializer)
        {
            var utcNow = _dateTimeProvider.UtcNow();
            _outQueueDispatcher
                .Dispatch(ConnectionId)
                .Produce(new OutPacket(
                     hookId: hookId,
                     channelId: channelId,
                     packetType: packetType,
                     connectionId: ConnectionId,
                     serializer: () => serializer(@event),
                     createdAt: utcNow,
                     destination: destination));
        }

        private async Task StartHeartbeat(
            CancellationToken cancellationToken)
        {
            if (!_hostClientSettingsInternal.HeartbeatDelayMs.HasValue)
            {
                return;
            }

            var heartbeatDelayMs = _hostClientSettingsInternal.HeartbeatDelayMs.Value;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!IsConnected && _dateTimeProvider.UtcNow() - _startConnect > _hostClientSettingsInternal.ConnectionTimeout)
                {
                    OnConnectionTimeout?.Invoke();
                    return;
                }

                var @event = new Heartbeat();

                SendInternal(
                    @event: @event,
                    packetType: PacketType.Protocol,
                    hookId: (byte)ProtocolHookId.Heartbeat,
                    destination: _hostClientSettingsInternal.ServerIpV4,
                    channelId: ReliableChannelConsts.ReliableChannel,
                    serializer: ProtocolEvent<Heartbeat>.Serialize);

                await Task.Delay(heartbeatDelayMs, cancellationToken).ConfigureAwait(false);
            }
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _cancellationTokenSource.Cancel();
                _outQueueDispatcher.Dispose();
            }

            _logger.Debug($"{this.GetType().Name} - disposed!");
            _disposed = true;
        }
    }
}
