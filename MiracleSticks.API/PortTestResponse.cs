using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace MiracleSticks.API
{
    [DataContract]
    public class PortTestResponse
    {
        [DataMember]
        public bool Success { get; set; }
    }
}