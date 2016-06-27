using System;
using System.Runtime.Serialization;

namespace MiracleSticks.API
{
    [DataContract]
    public class RegistrationRequest
    {
        public RegistrationRequest()
        {
            this.RequireRelay = false;
        }

        [DataMember]
        public string StickID { get; set; }

        [DataMember]
        public string GroupID { get; set; }

        [DataMember]
        public string ComputerName { get; set; }

        [DataMember]
        public int Port { get; set; }

        [DataMember]
        public bool RequireRelay { get; set; }
    }
}