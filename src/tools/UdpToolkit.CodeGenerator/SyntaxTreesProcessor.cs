﻿namespace UdpToolkit.CodeGenerator
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Shared code generator part between Roslyn and source generator.
    /// </summary>
    public static class SyntaxTreesProcessor
    {
        /// <summary>
        /// Generated file name.
        /// </summary>
        public const string GeneratedFileName = "HostWorkerGenerated.cs";

        /// <summary>
        /// Process.
        /// </summary>
        /// <param name="syntaxTrees">The parsed representations of a source documents.</param>
        /// <param name="filterByAttribute">Flag determines the needs filtering of classes by annotation attribute.</param>
        /// <returns>String representation of generated class.</returns>
        public static string Process(
            IEnumerable<SyntaxTree> syntaxTrees,
            bool filterByAttribute)
        {
            var allClasses = syntaxTrees
                .SelectMany(s => s.GetRoot().DescendantNodes())
                .Where(d => d.IsKind(SyntaxKind.ClassDeclaration))
                .OfType<ClassDeclarationSyntax>()
                .ToList();

            if (filterByAttribute)
            {
                allClasses = allClasses
                    .Select(cds => new
                    {
                        @class = cds,
                        attributes = cds.AttributeLists.SelectMany(al => al.Attributes),
                    })
                    .Where(tuple => tuple.attributes.Any(a => a.Name.ToString() == "UdpEvent"))
                    .Select(x => x.@class)
                    .ToList();
            }

            var sb = new StringBuilder();

            GenerateWarningsAboutAutoGeneratedFile(sb);
            GeneratePragma(sb);
            GenerateNamespaces(sb);
            GenerateUsings(sb);
            GenerateClassHeader(sb);
            GenerateLookup(sb, allClasses);
            GenerateProperties(sb);
            GenerateFields(sb);
            GenerateCtorMethod(sb);
            GenerateFinalizerMethod(sb);
            GeneratePublicDisposeMethod(sb);
            GenerateTryGetSubscriptionId(sb);
            GenerateProcessInPacketMethod(sb, allClasses);
            GenerateProcessOutPacketMethod(sb, allClasses);
            GenerateProcessExpiredPacketMethod(sb, allClasses);
            GeneratePrivateDisposeMethod(sb);
            GenerateClosedBraces(sb);

            return sb.ToString();
        }

        private static void GenerateTryGetSubscriptionId(StringBuilder sb)
        {
            sb.Append(@"
        public bool TryGetSubscriptionId(
            Type type,
            out byte subscriptionId)
        {
            return Lookup.TryGetValue(type, out subscriptionId);
        }
");
        }

        private static void GenerateWarningsAboutAutoGeneratedFile(StringBuilder sb)
        {
            sb.AppendLine($"// <auto-generated>");
            sb.AppendLine($"// THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.");
            sb.AppendLine($"// <auto-generated>");
            sb.AppendLine(string.Empty);
        }

        private static void GenerateLookup(StringBuilder sb, List<ClassDeclarationSyntax> allClasses)
        {
            sb.Append(@"
        private static readonly IReadOnlyDictionary<Type, byte> Lookup = new Dictionary<Type, byte>
        {");

            for (int i = 0; i < allClasses.Count; i++)
            {
                var @class = allClasses[i];
                var ns = GetNamespace(@class);
                var template = @"
            [typeof(TYPE)] = NUMBER,";
                var body = template
                    .Replace("NUMBER", i.ToString())
                    .Replace("TYPE", $"global::{ns.Name.ToString()}.{@class.Identifier.Text}");

                sb.Append(body);
            }

            sb.Append(@"
        };");
        }

        private static void GeneratePragma(StringBuilder sb)
        {
            sb.AppendLine($"#pragma warning disable");
            sb.AppendLine($"#pragma warning disable CS0105");
            sb.AppendLine($"#pragma warning disable S1128");
            sb.AppendLine($"#pragma warning disable SA1516");
            sb.AppendLine($"#pragma warning disable SA1200");
            sb.AppendLine($"#pragma warning disable SA1028");
            sb.AppendLine(string.Empty);
        }

        private static void GenerateNamespaces(StringBuilder sb)
        {
            sb.Append(@"namespace UdpToolkit.Framework
{");
        }

        private static void GenerateUsings(StringBuilder sb)
        {
            sb.Append(@"
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using UdpToolkit.Framework;
    using UdpToolkit.Framework.Contracts;
    using UdpToolkit.Serialization;
    using UdpToolkit.Network.Contracts.Clients;
");
        }

        private static void GenerateClassHeader(StringBuilder sb)
        {
            sb.Append(@"
    public sealed class HostWorkerGenerated : global::UdpToolkit.Framework.Contracts.IHostWorker
    {
");
        }

        private static void GenerateProperties(StringBuilder sb)
        {
            sb.Append(@"
        public global::UdpToolkit.Serialization.ISerializer Serializer { get; set; }
        public global::UdpToolkit.Framework.Contracts.IBroadcaster Broadcaster { get; set; }
");
        }

        private static void GenerateFields(StringBuilder sb)
        {
            sb.Append(@"
        private bool _disposed = false;
");
        }

        private static void GenerateCtorMethod(StringBuilder sb)
        {
            sb.Append(@"
        public HostWorkerGenerated()
        {
        }
        ");
        }

        private static void GenerateFinalizerMethod(StringBuilder sb)
        {
            sb.Append(@"
        ~HostWorkerGenerated()
        {
            Dispose(false);
        }
        ");
        }

        private static void GeneratePublicDisposeMethod(StringBuilder sb)
        {
            sb.Append(@"
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }");
        }

        private static void GenerateClosedBraces(StringBuilder sb)
        {
            var end = @"
    }
}";
            sb.Append(end);
        }

        private static void GeneratePrivateDisposeMethod(StringBuilder sb)
        {
            sb.Append(@"
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // nothing to dispose
            }

            _disposed = true;
        }");
        }

        private static void GenerateProcessInPacketMethod(StringBuilder sb, List<ClassDeclarationSyntax> allClasses)
        {
            sb.Append(@"
        public void Process(
            global::UdpToolkit.Network.Contracts.Clients.InNetworkPacket networkPacket)
        {
            switch (networkPacket.DataType)
            {");

            for (int i = 0; i < allClasses.Count; i++)
            {
                var @class = allClasses[i];
                var caseTemplate = @"
                case VALUE:
                {
                    var pooledObject = ObjectsPool<TYPE>.GetOrCreate();
                    try
                    {
                        var subscription = SubscriptionStorage<TYPE>.GetSubscription();
                        var payload = networkPacket.Buffer.AsSpan().Slice(0, networkPacket.BytesReceived);
                        var @event = Serializer.Deserialize<TYPE>(payload, pooledObject);

                        var groupId = subscription?.OnEvent?.Invoke(
                                networkPacket.ConnectionId,
                                networkPacket.IpV4Address,
                                @event);
                        
                        if (subscription.BroadcastMode != BroadcastMode.None)
                        {
                            Broadcaster.Broadcast<TYPE>(
                                caller: networkPacket.ConnectionId,
                                groupId: groupId.Value,
                                @event: @event,
                                channelId: networkPacket.ChannelId,
                                broadcastMode: subscription.BroadcastMode);
                        }
                        else
                        {
                            @event.Dispose();
                        }
                    }
                    catch (Exception)
                    {
                        pooledObject.Dispose();
                    }

                    break;
                }
             ";

                var ns = GetNamespace(@class);
                var body = caseTemplate
                    .Replace("VALUE", i.ToString())
                    .Replace("TYPE", $"global::{ns.Name.ToString()}.{@class.Identifier.Text}");

                sb.Append(body);
            }

            sb.Append(@"                 
                default:
                {
                    break;
                }
            }
        }");
        }

        private static void GenerateProcessExpiredPacketMethod(
            StringBuilder sb,
            List<ClassDeclarationSyntax> allClasses)
        {
            sb.Append(@"
        public void Process(
            in global::UdpToolkit.Network.Contracts.Packets.PendingPacket pendingPacket)
        {
            switch (pendingPacket.DataType)
            {");
            for (int i = 0; i < allClasses.Count; i++)
            {
                var @class = allClasses[i];
                var caseTemplate = @"
                case VALUE:
                {
                    var subscription = SubscriptionStorage<TYPE>.GetSubscription();
                    subscription?.OnTimeout?.Invoke();

                    break;    
                }
                 ";

                var ns = GetNamespace(@class);
                var body = caseTemplate
                    .Replace("VALUE", i.ToString())
                    .Replace("TYPE", $"global::{ns.Name.ToString()}.{@class.Identifier.Text}");

                sb.Append(body);
            }

            sb.Append(@"                 
                default:
                {
                    return;
                }
            }
        }");
        }

        private static void GenerateProcessOutPacketMethod(StringBuilder sb, List<ClassDeclarationSyntax> allClasses)
        {
            sb.Append(@"
        public void Process(
            global::UdpToolkit.Framework.Contracts.OutNetworkPacket outPacket)
        {
            switch (outPacket.DataType)
            {");

            for (int i = 0; i < allClasses.Count; i++)
            {
                var @class = allClasses[i];
                var caseTemplate = @"
                case VALUE:
                {
                    Serializer.Serialize<TYPE>(outPacket.BufferWriter, (TYPE)outPacket.Event);
                    return;
                }
                 ";

                var ns = GetNamespace(@class);
                var body = caseTemplate
                    .Replace("VALUE", i.ToString())
                    .Replace("TYPE", $"global::{ns.Name.ToString()}.{@class.Identifier.Text}");

                sb.Append(body);
            }

            sb.Append(@"                 
                default:
                {
                    return;
                }
            }
        }");
        }

        private static NamespaceDeclarationSyntax GetNamespace(SyntaxNode syntaxNode)
        {
            if (syntaxNode == null)
            {
                return null;
            }

            try
            {
                syntaxNode = syntaxNode.Parent;

                if (syntaxNode == null)
                {
                    return null;
                }

                if (syntaxNode is NamespaceDeclarationSyntax)
                {
                    return syntaxNode as NamespaceDeclarationSyntax;
                }

                return GetNamespace(syntaxNode);
            }
            catch
            {
                return null;
            }
        }
    }
}