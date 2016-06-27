using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using MiracleSticks.PacketRelay;

namespace MiracleSticksServer.Relay
{
    /// <summary>
    /// This version of the socket relay does not connect to the other endpoint until a message is received from the connected endpoint.
    /// </summary>
    public class DelayedConnectRelay : TcpSocketRelay
    {
        private static readonly byte[] ConnectMsgBytes = Encoding.UTF8.GetBytes(SessionManager.ClientConnectedMsg);
        private static readonly byte[] DisconnectMsgBytes = Encoding.UTF8.GetBytes(SessionManager.ClientDisconnectedMsg);

        public delegate void VoidDelegate();

        public event VoidDelegate ClientConnected;
        public event VoidDelegate ClientDisconnected;

        public DelayedConnectRelay(Dictionary<string, RelaySession> sessions)
            : base(sessions)
        {
        }

        protected override void ProcessIncomingData(RelaySession session, TcpClient recvClient, byte[] buffer, int bytesRead)
        {
            if (bytesRead == ConnectMsgBytes.Length && 
                Encoding.UTF8.GetString(buffer, 0, bytesRead) == SessionManager.ClientConnectedMsg)
            {
                ClientConnected();

                object[] recvSession = new object[] { session, recvClient, buffer };
                recvClient.Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, HandleIncomingData, recvSession);
            }
            else if(bytesRead == DisconnectMsgBytes.Length && 
                Encoding.UTF8.GetString(buffer, 0, bytesRead) == SessionManager.ClientDisconnectedMsg)
            {
                ClientDisconnected();

                object[] recvSession = new object[] { session, recvClient, buffer };
                recvClient.Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, HandleIncomingData, recvSession);
            }
            else
            {
                base.ProcessIncomingData(session, recvClient, buffer, bytesRead);   
            }
        }
    }
}
