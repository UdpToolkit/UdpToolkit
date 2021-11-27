namespace UdpToolkit.Network.Connections
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using UdpToolkit.Logging;
    using UdpToolkit.Network.Contracts.Connections;
    using UdpToolkit.Network.Contracts.Sockets;
    using UdpToolkit.Network.Utils;

    /// <inheritdoc />
    public sealed class ConnectionPool : IConnectionPool
    {
        private readonly ConcurrentDictionary<Guid, IConnection> _connections = new ConcurrentDictionary<Guid, IConnection>();
        private readonly ILogger _logger;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IConnectionFactory _connectionFactory;
        private readonly TimeSpan _inactivityTimeout;
        private readonly Timer _housekeeper;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPool"/> class.
        /// </summary>
        /// <param name="dateTimeProvider">Instance of date time provider.</param>
        /// <param name="logger">Instance of logger.</param>
        /// <param name="settings">Instance of settings.</param>
        /// <param name="connectionFactory">Instance of connection factory.</param>
        public ConnectionPool(
            IDateTimeProvider dateTimeProvider,
            ILogger logger,
            ConnectionPoolSettings settings,
            IConnectionFactory connectionFactory)
        {
            _dateTimeProvider = dateTimeProvider;
            _inactivityTimeout = settings.ConnectionTimeout;
            _connectionFactory = connectionFactory;
            _logger = logger;
            _housekeeper = new Timer(
                callback: ScanForCleaningInactiveConnections,
                state: null,
                dueTime: TimeSpan.FromSeconds(10),
                period: settings.ConnectionsCleanupFrequency);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ConnectionPool"/> class.
        /// </summary>
        [ExcludeFromCodeCoverage]
        ~ConnectionPool()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void Remove(
            IConnection connection)
        {
            _connections.TryRemove(connection.ConnectionId, out _);
        }

        /// <inheritdoc />
        public bool TryGetConnection(
            Guid connectionId,
            out IConnection connection)
        {
            return _connections.TryGetValue(connectionId, out connection);
        }

        /// <inheritdoc />
        public IConnection GetOrAdd(
            Guid connectionId,
            bool keepAlive,
            DateTimeOffset lastHeartbeat,
            IpV4Address ipV4Address)
        {
            // MEMORY OPTIMIZATION: avoid usage GetOrAdd method
            // https://www.meziantou.net/concurrentdictionary-closure.html
            if (_connections.TryGetValue(connectionId, out var connection))
            {
                return connection;
            }

            var newConnection = _connectionFactory.Create(connectionId, keepAlive, lastHeartbeat, ipV4Address);
            if (_connections.TryAdd(connectionId, newConnection))
            {
                return newConnection;
            }

            return null;
        }

        private void ScanForCleaningInactiveConnections(object state)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.Debug($"[UdpToolkit.Network] Cleanup inactive connections");
            }

            var now = _dateTimeProvider.GetUtcNow();
            for (var i = 0; i < _connections.Count; i++)
            {
                var connection = _connections.ElementAt(i);
                if (connection.Value.KeepAlive)
                {
                    continue;
                }

                var inactivityDiff = now - connection.Value.LastHeartbeat;
                if (inactivityDiff > _inactivityTimeout)
                {
#pragma warning disable S1066
                    if (_connections.TryRemove(connection.Key, out var removedConnection) && _logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.Debug($"[UdpToolkit.Network] Connection: {removedConnection.ConnectionId} removed by inactivity timeout: {_inactivityTimeout}, idle time: {inactivityDiff}");
                    }
#pragma warning restore S1066
                }
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
                _housekeeper.Dispose();
            }

            _disposed = true;
        }
    }
}