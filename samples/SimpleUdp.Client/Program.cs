﻿using System;
using System.Threading;
using System.Threading.Tasks;
using SimpleUdp.Contracts;
using UdpToolkit.Core;
using UdpToolkit.Framework.Hosts;
using UdpToolkit.Serialization.MsgPack;

namespace SimpleUdp.Client
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var clientHost = BuildClientHost();

            var consumerFactory = clientHost.GetEventConsumerFactory();
            var producerFactory = clientHost.GetEventProducerFactory();
            
            var consumer = consumerFactory.Create<SumEvent>();
            var producer = producerFactory.Create<AddEvent>(scopeId: 0);

            new Thread(() => Consume(consumer)).Start();
            new Thread(() => Produce(producer)).Start();
            
            await clientHost.RunAsync();
        }

        private static void Produce(IEventProducer<AddEvent> producer)
        {
            var x = 5;
            var y = 7;
            
            while (true)
            {
                producer.Produce(new AddEvent
                {
                    X = x,
                    Y = y
                });
                
                Console.WriteLine($"Event {nameof(AddEvent)} produced with values {x} {y}!");
                
                Thread.Sleep(1000);
            }
        }
        private static void Consume(IEventConsumer<SumEvent> consumer)
        {
            while (true)
            {
                var events = consumer.Consume();
                foreach (var @event in events)
                {
                    Console.WriteLine($"Event {nameof(SumEvent)} consumed with value {@event.Sum}!");    
                }
                
                Thread.Sleep(1000);
            }
        }

        private static IClientHost BuildClientHost()
        {
            return Host
                .CreateClientBuilder()
                .Configure(cfg =>
                {
                    cfg.ServerHost = "0.0.0.0";
                    cfg.Serializer = new Serializer();
                    cfg.ServerInputPorts = new[] { 7000, 7001 };
                    cfg.ServerOutputPorts = new[] { 8000, 8001 };
                    cfg.Receivers = 2;
                    cfg.Senders = 2;
                })
                .Build();
        }
    }
}
