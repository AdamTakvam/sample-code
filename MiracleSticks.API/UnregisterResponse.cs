using System;
using System.Runtime.Serialization;

namespace MiracleSticks.API
{
    [DataContract]
    public class UnregisterResponse
    {
        public UnregisterResponse()
        {
            Success = false;
        }

        [DataMember]
        public bool Success { get; set; }
    }
}