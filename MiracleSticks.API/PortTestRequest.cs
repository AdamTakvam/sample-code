using System;
using System.Runtime.Serialization;

namespace MiracleSticks.API
{
    [DataContract]
    public class PortTestRequest
    {
        [DataMember]
        public int Port { get; set; }
    }
}