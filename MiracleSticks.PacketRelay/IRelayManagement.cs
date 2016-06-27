using System;
using System.ServiceModel;

namespace MiracleSticks.PacketRelay
{
    [ServiceContract]
    public interface IRelayManagement
    {
        [OperationContract]
        bool Ping();

        [OperationContract]
        RelayConnectResponse HalfConnect(string sessionId, string serverIP);

        [OperationContract]
        RelayConnectResponse FullConnect(string sessionId, string clientIP);
    }
}
