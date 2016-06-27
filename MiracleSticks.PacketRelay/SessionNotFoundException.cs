using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiracleSticks.PacketRelay
{
    internal class SessionNotFoundException : RelayConnectException
    {
        public SessionNotFoundException(string sessionId)
            : base(sessionId, "Session ID not found.") { }
    }
}
