namespace UdpToolkit.Core
{
    using System;

    public interface IContainerBuilder
    {
        IContainerBuilder RegisterSingleton<TInterface, TService>(TService instance)
            where TService : TInterface;

        IContainerBuilder RegisterSingleton<TInterface, TService>(Func<TService> factory)
            where TService : TInterface;

        IContainer Build();
    }
}