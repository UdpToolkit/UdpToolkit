﻿namespace Structs.Client
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Serializers;
    using Structs.Contracts;
    using UdpToolkit;
    using UdpToolkit.Framework;
    using UdpToolkit.Framework.Contracts;
    using UdpToolkit.Network.Channels;
    using UdpToolkit.Network.Sockets;

    public static class Program
    {
        private static readonly Guid GroupId = Guid.Parse("4a45a988-aee8-43e1-8aa2-805868b20bac");
        private static readonly Guid ConnectionId = Guid.NewGuid();
        private static bool _isStarted = false;
        private static bool _isOver = false;

        public static void Main(string[] args)
        {
            var nickname = args[0];

            using var host = BuildHost();
            var client = host.HostClient;

            var isConnected = false;
            var groupManager = host.ServiceProvider.GroupManager;

            host.HostClient.OnConnectionTimeout += () =>
            {
                Console.WriteLine($"ConnectionTimeout for - {nickname}");
                isConnected = false;
            };
            host.HostClient.OnRttReceived += rtt => Console.WriteLine($"{nickname} rtt - {rtt}");
            host.HostClient.OnConnected += (ipV4, connectionId) =>
            {
                isConnected = true;
                Console.WriteLine($"{nickname} connected with id - {connectionId}");
            };
            host.HostClient.OnDisconnected += (ipV4, connectionId) =>
            {
                isConnected = false;
                Console.WriteLine($"{nickname} disconnected with id - {connectionId}");
            };

            host.On<StartGame>(
                onEvent: (connectionId, ip, startGame) =>
                {
                    _isStarted = true;

                    groupManager.JoinOrCreate(startGame.GroupId, connectionId);
                });

            host.On<GameOver>(
                onEvent: (connectionId, ip, gameOver) =>
                {
                    Console.WriteLine("End of Game!");
                    _isOver = true;
                });

            host.On<Respawn>(
                onEvent: (connectionId, ip, respawn) =>
                {
                    Console.WriteLine($"Respawn for {respawn.Nickname}!");
                });

            host.Run();

            client.Connect(ConnectionId, ConnectionId);

            var waitTimeout = TimeSpan.FromSeconds(120);
            SpinWait.SpinUntil(() => isConnected, waitTimeout);
            Console.WriteLine($"IsConnected - {isConnected}");

#pragma warning disable CS4014
            Task.Run(async () =>
#pragma warning restore CS4014
            {
                while (true)
                {
                    await Task.Delay(3000).ConfigureAwait(false);
                    client.Ping();
                }
            });

            client.SendUnmanaged(
                @event: new JoinEvent(GroupId, nickname.GetHashCode()),
                channelId: ReliableChannel.Id);

            SpinWait.SpinUntil(() => _isStarted, waitTimeout);
            Console.WriteLine($"Game started!");

            for (var i = 0; i < 3; i++)
            {
                client.SendUnmanaged(
                    @event: new Death(nickname.GetHashCode(), GroupId),
                    channelId: ReliableChannel.Id);

                Thread.Sleep(1000);
            }

            SpinWait.SpinUntil(() => _isOver, waitTimeout);
            Console.WriteLine($"Game over!");

            client.Disconnect();
            SpinWait.SpinUntil(() => !isConnected, waitTimeout);
            Console.WriteLine($"Client closed!");
        }

        private static IHost BuildHost()
        {
            var hostSettings = new HostSettings(
                serializer: new UnsafeSerializer());

            return UdpHost
                .CreateHostBuilder()
                .ConfigureHost(hostSettings, (settings) =>
                {
                    settings.Host = "127.0.0.1";
                    settings.HostPorts = new[] { 0 };
                    settings.Workers = 8;
                    settings.Executor = new TaskBasedExecutor();
                    settings.ResendPacketsInterval = TimeSpan.FromSeconds(1);
                })
                .ConfigureHostClient((settings) =>
                {
                    settings.ConnectionTimeout = TimeSpan.FromSeconds(150);
                    settings.ServerHost = "127.0.0.1";
                    settings.ServerPorts = new[] { 7000, 7001 };
                })
                .ConfigureNetwork((settings) =>
                {
                    settings.ChannelsFactory = new ChannelsFactory();
                    settings.SocketFactory = new NativeSocketFactory();
                    settings.ConnectionTimeout = TimeSpan.FromSeconds(120);
                    settings.ResendTimeout = TimeSpan.FromSeconds(20);
                })
                .BootstrapWorker(new HostWorkerGenerated())
                .Build();
        }
    }
}