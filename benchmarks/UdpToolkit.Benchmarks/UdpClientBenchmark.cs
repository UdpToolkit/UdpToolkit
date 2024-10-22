namespace UdpToolkit.Benchmarks
{
    using System;
    using System.Buffers;
    using System.Linq;
    using BenchmarkDotNet.Attributes;
    using UdpToolkit.Benchmarks.Fakes;
    using UdpToolkit.Logging;
    using UdpToolkit.Network.Channels;
    using UdpToolkit.Network.Clients;
    using UdpToolkit.Network.Connections;
    using UdpToolkit.Network.Contracts.Channels;
    using UdpToolkit.Network.Contracts.Clients;
    using UdpToolkit.Network.Contracts.Sockets;
    using UdpToolkit.Network.Sockets;
    using UdpToolkit.Network.Utils;

    [HtmlExporter]
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    public class UdpClientBenchmark
    {
#pragma warning disable SA1401

        [Params(100, 1000)]
        public int Repeats;
#pragma warning restore SA1401

        private static readonly byte[] Bytes = Enumerable.Range(0, 100).Select(x => (byte)x).ToArray();
        private static readonly byte[] ReceiveBuffer = new byte[1500];

        private Guid _connectionId;
        private IUdpClient _udpClient;
        private IpV4Address _destination;
        private IpV4Address _remote = new IpV4Address(0, 0);

        [IterationSetup]
        public void Setup()
        {
            _connectionId = Guid.NewGuid();
            _udpClient = this
                .CreateFactory(
                    repeats: Repeats,
                    socketFactory: new NativeSocketFactory(),
                    channelsFactory: new FakeChannelsFactory())
                .Create(ipV4Address: new IpV4Address(0, 0));
            _destination = new IpV4Address(0, 0);

            // add connection to pool
            _udpClient.Connect(_destination, _connectionId);
        }

        [IterationSetup(Target = nameof(Poll))]
        public void PollSetup()
        {
            _connectionId = Guid.NewGuid();
            _udpClient = this
                .CreateFactory(
                    repeats: Repeats,
                    socketFactory: new FakeSocketFactory(),
                    channelsFactory: new FakeChannelsFactory())
                .Create(ipV4Address: new IpV4Address(0, 0));
            _destination = new IpV4Address(0, 0);
        }

        [Benchmark]
        public void Connect()
        {
            for (int i = 0; i < Repeats; i++)
            {
                _udpClient.Connect(_destination, _connectionId);
            }
        }

        [Benchmark]
        public void Disconnect()
        {
            for (int i = 0; i < Repeats; i++)
            {
                _udpClient.Disconnect(_destination);
            }
        }

        [Benchmark]
        public void Heartbeat()
        {
            for (int i = 0; i < Repeats; i++)
            {
                _udpClient.Heartbeat(_destination);
            }
        }

        [Benchmark]
        public void Send()
        {
            for (var i = 0; i < Repeats; i++)
            {
                _udpClient.Send(
                    connectionId: _connectionId,
                    channelId: SequencedChannel.Id,
                    dataType: 1,
                    payload: Bytes.AsSpan(),
                    ipV4Address: _destination);
            }
        }

        [Benchmark]
        public void Poll()
        {
            for (var i = 0; i < Repeats; i++)
            {
                _udpClient.Send(
                    connectionId: _connectionId,
                    channelId: SequencedChannel.Id,
                    dataType: 1,
                    payload: Bytes.AsSpan(),
                    ipV4Address: _destination);

                _udpClient.Poll(2, ref _remote, ReceiveBuffer);
            }
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _udpClient.Dispose();
        }

        private IUdpClientFactory CreateFactory(
            int repeats,
            ISocketFactory socketFactory,
            IChannelsFactory channelsFactory)
        {
            return new UdpClientFactory(
                udpClientSettings: new UdpClientSettings(
                    mtuSizeLimit: 1500,
                    udpClientBufferSize: 2048,
                    pollFrequency: 15,
                    allowIncomingConnections: true,
                    resendTimeout: TimeSpan.FromSeconds(3),
                    channelsFactory: channelsFactory,
                    socketFactory: socketFactory,
                    packetsPoolSize: repeats + 1,
                    packetsBufferPoolSize: repeats + 1,
                    headersBuffersPoolSize: repeats + 1,
                    arrayPool: ArrayPool<byte>.Create(maxArrayLength: 2048, maxArraysPerBucket: repeats)),
                connectionPool: new ConnectionPool(
                    dateTimeProvider: new DateTimeProvider(),
                    logger: new SimpleConsoleLogger(LogLevel.DisableLogs),
                    settings: new ConnectionPoolSettings(
                        connectionTimeout: TimeSpan.FromMinutes(15),
                        connectionsCleanupFrequency: TimeSpan.FromMinutes(15)),
                    connectionFactory: new ConnectionFactory(new ChannelsFactory())),
                loggerFactory: new SimpleConsoleLoggerFactory(LogLevel.Error),
                dateTimeProvider: new DateTimeProvider());
        }
    }
}