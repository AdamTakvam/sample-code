using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiracleSticks.PacketRelay
{
    internal class RelayConnectException : Exception
    {
        public RelayConnectException(string sessionId, string message)
            : base(message)
        {
            SessionId = sessionId;
        }

        public string SessionId { get; set; }
    }
}
