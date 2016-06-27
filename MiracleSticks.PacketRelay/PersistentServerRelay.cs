using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MiracleSticks.PacketRelay
{
    /// <summary>
    /// This version of the socket relay sends a message when the client disconnects instead of terminating the server connection.
    /// </summary>
    public class PersistentServerRelay : TcpSocketRelay
    {
        public PersistentServerRelay(Dictionary<String, RelaySession> sessions)
            : base(sessions)
        {
        }

        protected override void OnTcpClientDisconnected(System.Net.Sockets.TcpClient tcpClient, RelaySession session)
        {
            if(tcpClient == session.ClientSocket)
            {
                if(DebugLog != null)
                    DebugLog.Write(TraceLevel.Info, "Remote client disconnected. Sending disconnect message to server.");

                // Tell server that the client has disconnected
                byte[] buffer = Encoding.UTF8.GetBytes(SessionManager.ClientDisconnectedMsg);
                SendData(session, null, session.ServerSocket, buffer, buffer.Length);
            }

            base.OnTcpClientDisconnected(tcpClient, session);
        }
    }
}
