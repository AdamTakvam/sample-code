using System;
using System.Runtime.Serialization;

namespace MiracleSticks.API
{
    [DataContract]
    public class ConnectRequest
    {
        [DataMember]
        public string SessionId { get; set; }

        [DataMember]
        public string GroupID { get; set; }
    }
}