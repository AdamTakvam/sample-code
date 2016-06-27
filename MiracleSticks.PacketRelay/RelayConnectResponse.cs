using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace MiracleSticks.PacketRelay
{
    [DataContract]
    public class RelayConnectResponse
    {
        public enum ResultCode
        {
            Success,
            UnknownSession,
            Failure
        }

        [DataMember]
        public ResultCode Result { get; set; }

        [DataMember]
        public string IPAddress { get; set; }

        [DataMember]
        public int Port { get; set; }

        [DataMember]
        public string Description { get; set; }
    }
}
