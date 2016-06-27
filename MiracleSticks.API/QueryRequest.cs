using System;
using System.Runtime.Serialization;

namespace MiracleSticks.API
{
    [DataContract]
    public class QueryRequest
    {
        [DataMember]
        public string GroupID { get; set; }
    }
}