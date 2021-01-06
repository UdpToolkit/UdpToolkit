﻿namespace ReliableUdp.Server
{
    using System;
    using System.Threading.Tasks;
    using ReliableUdp.Contracts;
    using Serilog;
    using Serilog.Events;
    using UdpToolkit;
    using UdpToolkit.Core;
    using UdpToolkit.Serialization.MsgPack;

    public static class Program
    {
        public static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Debug)
                .WriteTo.Console()
                .CreateLogger();

            var host = BuildHost();

            host.On<JoinEvent>(
                onEvent: (peerId, joinEvent, roomManager) =>
                {
                    roomManager
                        .JoinOrCreate(joinEvent.RoomId, peerId);

                    Log.Logger.Information($"{joinEvent.Nickname} joined to room!");

                    return joinEvent.RoomId;
                },
                scheduleCall: (peerId, joinEvent, scheduler) =>
                {
                    scheduler
                        .Schedule(
                            roomId: joinEvent.RoomId,
                            timerId: Timers.JoinTimeout,
                            dueTimeMs: 20_000,
                            action: () =>
                            {
                                Log.Logger.Information($"Scheduled event!");
                                host.PublishCore(new StartGame(joinEvent.RoomId), joinEvent.RoomId, 1, UdpMode.ReliableUdp);
                            });
                },
                onAck: (peerId) => { },
                onTimeout: (peerId) => { },
                broadcastMode: BroadcastMode.Room,
                hookId: 0);

            host.On<StartGame>(
                onEvent: (peerId, startGame) =>
                {
                    Log.Logger.Information("Game started!");

                    return startGame.RoomId;
                },
                onAck: (peerId) =>
                {
                    Log.Logger.Information("Game started ack!");
                },
                onTimeout: (peerId) => { },
                broadcastMode: BroadcastMode.Room,
                hookId: 1);

            await host
                .RunAsync()
                .ConfigureAwait(false);
        }

        private static IHost BuildHost() =>
            UdpHost
                .CreateHostBuilder()
                .ConfigureHost(settings =>
                {
                    settings.Host = "127.0.0.1";
                    settings.Serializer = new Serializer();
                    settings.InputPorts = new[] { 7000, 7001 };
                    settings.OutputPorts = new[] { 8000, 8001 };
                    settings.Workers = 2;
                    settings.ResendPacketsTimeout = TimeSpan.FromSeconds(120);
                    settings.PeerInactivityTimeout = TimeSpan.FromSeconds(120);
                })
                .ConfigureServerHostClient((settings) =>
                {
                    settings.ResendPacketsTimeout = TimeSpan.FromSeconds(120);
                    settings.ConnectionTimeout = TimeSpan.FromSeconds(120);
                    settings.ClientHost = "127.0.0.1";
                    settings.ServerHost = "127.0.0.1";
                    settings.ServerInputPorts = new[] { 7000, 7001 };
                    settings.PingDelayInMs = null; // pass null for disable pings
                })
                .Build();
    }
}
