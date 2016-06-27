using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MiracleSticks.PacketRelay
{
    public class RelaySession
    {
        public RelaySession(string sessionId)
        {
            Debug.Assert(!String.IsNullOrEmpty(sessionId));
            SessionId = sessionId;

            DataBackLog = new MemoryStream();
        }

        public string SessionId { get; private set; }

        public IPAddress ServerIP { get; set; }

        public IPAddress ClientIP { get; set; }

        public TcpClient ServerSocket { get; set; }

        public TcpClient ClientSocket { get; set; }

        public DateTime ServerReserveTime { get; set; }

        public DateTime ClientReserveTime { get; set; }

        public bool ServerConnected { get { return ServerSocket != null; } }

        public bool ClientConnected { get { return ClientSocket != null; } }

        public MemoryStream DataBackLog { get; private set; }
    }
}
