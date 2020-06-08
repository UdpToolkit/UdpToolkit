namespace UdpToolkit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using UdpToolkit.Core;
    using UdpToolkit.Framework.Server.Core;
    using UdpToolkit.Framework.Server.Peers;
    using UdpToolkit.Framework.Server.Rpcs;
    using UdpToolkit.Tests.Resources;
    using UdpToolkit.Utils;
    using Xunit;

    public class RpcProviderTests
    {
        public static IEnumerable<object[]> Cases()
        {
            yield return new object[]
            {
                1,
                0,
                new object[]
                {
                    new TestService(),
                },
                new object[0],
            };

            yield return new object[]
            {
                1,
                1,
                new object[]
                {
                    new TestService(),
                },
                new object[]
                {
                    new Message(100, 001),
                },
            };

            yield return new object[]
            {
                1,
                2,
                new object[]
                {
                    new TestService(),
                },
                new object[]
                {
                    1,
                    1,
                },
            };

            yield return new object[]
            {
                0,
                0,
                new object[0],
                new object[0],
            };

            yield return new object[]
            {
                0,
                1,
                new object[0],
                new object[]
                {
                    new Message(100, 001),
                },
            };

            yield return new object[]
            {
                0,
                2,
                new object[0],
                new object[]
                {
                    1,
                    1,
                },
            };
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task RpcProvider_InvokeHubRpc_NotThrown_Async(byte hubId, byte rpcId, object[] ctorArgs, object[] methodArgs)
        {
            var rpcTransformer = new RpcTransformer();
            var methods = MethodDescriptorStorage.HubMethods.ToArray();

            var rpcs = methods
                .Select(x => rpcTransformer.Transform(x))
                .ToList()
                .ToDictionary(rpcDesc => rpcDesc.RpcDescriptorId);

            IRpcProvider rpcProvider = new RpcProvider(rpcs);

            var key = new RpcDescriptorId(hubId: hubId, rpcId: rpcId);

            rpcProvider.TryProvide(key, out var rpcDescriptor);

            var exception = await Record
                .ExceptionAsync(() => rpcDescriptor.HubRpc(
                    hubContext: new HubContext(Guid.NewGuid(), 0, 0, UdpMode.Udp),
                    hubClients: null,
                    roomManager: new RoomManager(new PeerManager(new DateTimeProvider())),
                    ctorArguments: ctorArgs,
                    methodArguments: methodArgs))
                .ConfigureAwait(false);

            Assert.Null(exception);
        }
    }
}
