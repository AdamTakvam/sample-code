using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MiracleSticks.API
{
    [DataContract]
    public class QueryResponse
    {
        public QueryResponse()
        {
            Success = false;
            Servers = new List<ServerRegistration>();
        }

        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public List<ServerRegistration> Servers { get; set; }
    }
}