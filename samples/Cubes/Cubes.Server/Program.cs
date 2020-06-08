﻿namespace Cubes.Server
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Cubes.Server.Services;
    using Serilog;
    using Serilog.Events;
    using UdpToolkit.Framework.Server.Core;
    using UdpToolkit.Framework.Server.Di.Autofac;
    using UdpToolkit.Framework.Server.Pipelines;
    using UdpToolkit.Serialization.MsgPack;
    using UdpToolkit.Utils;

    public static class Program
    {
        public static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Debug)
                .WriteTo.Console()
                .CreateLogger();

            var server = BuildServer();

            await server.RunAsync().ConfigureAwait(false);
        }

        private static IServerHost BuildServer() =>
            AutofacHost
                .CreateServerBuilder(builder =>
                {
                    builder
                        .RegisterType<GameService>()
                        .As<IGameService>()
                        .SingleInstance();
                })
                .Configure(settings =>
                {
                    settings.ServerHost = "0.0.0.0";
                    settings.InputPorts = new[] { 7000, 7001 };
                    settings.OutputPorts = new[] { 8000, 8001 };
                    settings.ProcessWorkers = 2;
                    settings.Serializer = new Serializer();
                    settings.CacheOptions = new CacheOptions(
                        roomTtl: TimeSpan.FromMinutes(1),
                        peerTtl: Timeout.InfiniteTimeSpan);
                })
                .Use(pipeline =>
                {
                    pipeline
                        .Append<ProcessStage>();
                })
                .Build();
    }
}