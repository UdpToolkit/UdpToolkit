﻿namespace P2P.Client.B
{
    using System;
    using System.Threading;
    using Serilog;
    using Serilog.Events;
    using UdpToolkit;
    using UdpToolkit.Core;
    using UdpToolkit.Logging.Serilog;
    using UdpToolkit.Serialization.MsgPack;

    public static class Program
    {
        public static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Debug)
                .WriteTo.Console()
                .CreateLogger();

            var host = BuildHost();
            var client = host.HostClient;

            host.Run();

            var connectionTimeout = TimeSpan.FromSeconds(120);
            SpinWait.SpinUntil(() => client.IsConnected, connectionTimeout);

            Console.WriteLine("Press any key...");
            Console.ReadLine();
        }

        private static IHost BuildHost()
        {
            return UdpHost
                .CreateHostBuilder()
                .ConfigureHost((settings) =>
                {
                    settings.Host = "127.0.0.1";
                    settings.Serializer = new Serializer();
                    settings.LoggerFactory = new SerilogLoggerFactory();
                    settings.HostPorts = new[] { 5000, 5001 };
                    settings.Workers = 2;
                    settings.ResendPacketsTimeout = TimeSpan.FromSeconds(120);
                    settings.ConnectionTtl = TimeSpan.FromSeconds(120);
                })
                .ConfigureHostClient((settings) =>
                {
                    settings.ConnectionTimeout = TimeSpan.FromSeconds(120);
                    settings.ServerHost = "127.0.0.1";
                    settings.ServerPorts = new[] { 7000, 7001 };
                    settings.HeartbeatDelayInMs = null; // pass null for disable heartbeat
                })
                .Build();
        }
    }
}
