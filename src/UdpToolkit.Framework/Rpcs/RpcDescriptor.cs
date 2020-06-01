namespace UdpToolkit.Framework.Rpcs
{
    using System;
    using System.Collections.Generic;
    using UdpToolkit.Core;
    using UdpToolkit.Framework.Hubs;

    public readonly struct RpcDescriptor
    {
        public RpcDescriptor(
            HubRpc hubRpc,
            RpcDescriptorId rpcDescriptorId,
            IReadOnlyCollection<Type> parametersTypes,
            IReadOnlyCollection<Type> ctorArguments)
        {
            HubRpc = hubRpc;
            ParametersTypes = parametersTypes;
            CtorArguments = ctorArguments;
            RpcDescriptorId = rpcDescriptorId;
        }

        public IReadOnlyCollection<Type> CtorArguments { get; }

        public IReadOnlyCollection<Type> ParametersTypes { get; }

        public HubRpc HubRpc { get; }

        public RpcDescriptorId RpcDescriptorId { get; }
    }
}