using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace MiracleSticks.API
{
    [ServiceContract]
    public interface IMiracleSticksAPI
    {
        [OperationContract]
        RegistrationResponse Register(RegistrationRequest request);

        [OperationContract]
        UnregisterResponse Unregister(UnregisterRequest request);

        [OperationContract]
        QueryResponse Query(QueryRequest request);

        [OperationContract]
        ConnectResponse Connect(ConnectRequest request);

        [OperationContract]
        PortTestResponse PortTest(PortTestRequest request);
    }
}
