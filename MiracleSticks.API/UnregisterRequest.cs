using System;
using System.Runtime.Serialization;

namespace MiracleSticks.API
{
    [DataContract]
    public class UnregisterRequest
    {
        [DataMember]
        public string GroupID { get; set; }

        [DataMember]
        public string ComputerName { get; set; }
    }
}