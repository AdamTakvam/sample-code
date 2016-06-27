using System;
using System.Runtime.Serialization;

namespace MiracleSticks.API
{
    [DataContract]
    public class ConnectResponse
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string RelayIPAddress { get; set; }

        [DataMember]
        public int RelayPort { get; set; }

        [DataMember]
        public string Description { get; set; }
    }
}