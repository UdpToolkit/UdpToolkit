namespace UdpToolkit.Core
{
    using System.Net;

    public interface IServerSelector
    {
        IPEndPoint GetServer();
    }
}