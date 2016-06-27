using System;
using System.Runtime.Serialization;

namespace MiracleSticks.API
{
    [DataContract]
    public class ServerRegistration
    {
        [DataMember]
        public string SessionId { get; set; }

        [DataMember]
        public string ComputerName { get; set; }

        [DataMember]
        public string IPAddress { get; set; }

        [DataMember]
        public int Port { get; set; }
    }
}