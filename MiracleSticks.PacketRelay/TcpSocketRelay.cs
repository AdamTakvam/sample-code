using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using MiracleSticks.Logging;

namespace MiracleSticks.PacketRelay
{
    public class TcpSocketRelay : IDisposable
    {
        private const int BufferSize = 1500; // Ethernet MTU

        private readonly Dictionary<string, RelaySession> sessions;

        public ILogger DebugLog { get; set; }

        public TcpSocketRelay(Dictionary<string, RelaySession> sessions)
        {
            if(sessions == null)
                throw new ArgumentNullException("sessions");

            this.sessions = sessions;
        }

        public virtual void AddClient(RelaySession session, TcpClient tcpClient)
        {
            byte[] buffer = new byte[BufferSize];  // Match ethernet MTU
            object[] socketSession = new object[] { session, tcpClient, buffer };

            if(session.DataBackLog.Length > 0)
            {
                if (DebugLog != null)
                    DebugLog.Write(TraceLevel.Info, "Sending backlog data to: " + tcpClient.Client.RemoteEndPoint);

                tcpClient.Client.Send(session.DataBackLog.GetBuffer(), 0, Convert.ToInt32(session.DataBackLog.Length), SocketFlags.None);

                session.DataBackLog.SetLength(0);
                session.DataBackLog.Seek(0, SeekOrigin.Begin);
            }

            tcpClient.Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, HandleIncomingData, socketSession);
        }

        public virtual void KillSession(RelaySession session)
        {
            if(session != null)
            {
                if(session.ServerSocket != null)
                {
                    try { session.ServerSocket.Close(); } catch {}
                    session.ServerSocket = null;
                }
            
                if(session.ClientSocket != null)
                {
                    try { session.ClientSocket.Close(); } catch {}
                    session.ClientSocket = null;
                }

                sessions.Remove(session.SessionId);
            }
        }

        public void SendData(RelaySession session, TcpClient recvClient, TcpClient sendClient, byte[] buffer, int byteCount)
        {
            if(buffer.Length != BufferSize)
                Array.Resize(ref buffer, BufferSize);

            var sendArgs = new SocketAsyncEventArgs();
            sendArgs.UserToken = new object[] { session, recvClient, sendClient, buffer };
            sendArgs.Completed += OnSendComplete;
            sendArgs.SetBuffer(buffer, 0, byteCount);
             
            if (!sendClient.Client.SendAsync(sendArgs))
            {
                // Completed synchronously
                OnSendComplete(null, sendArgs);
            }
        }

        public virtual void Dispose()
        {
            lock (sessions)
            {
                foreach(RelaySession session in sessions.Values)
                {
                    KillSession(session);
                }
            }
        }
        
        protected virtual void HandleIncomingData(IAsyncResult ar)
        {
            var socketSession = ar.AsyncState as object[];
            if (socketSession != null && socketSession.Length >= 3)
            {
                RelaySession session = socketSession[0] as RelaySession;
                TcpClient recvClient = socketSession[1] as TcpClient;
                byte[] buffer = socketSession[2] as byte[];

                if (buffer != null && 
                    session != null && 
                    recvClient != null && 
                    recvClient.Client != null)
                {
                    try
                    {
                        int bytesRead = recvClient.Client.EndReceive(ar);

                        if (bytesRead == 0)
                        {
                            if (DebugLog != null)
                                DebugLog.Write(TraceLevel.Info, "Endpoint disconnected: " + recvClient.Client.RemoteEndPoint);

                            OnTcpClientDisconnected(recvClient, session);
                        }
                        else
                        {
                            ProcessIncomingData(session, recvClient, buffer, bytesRead);
                        }
                    }
                    catch (SocketException)
                    {
                        if (DebugLog != null)
                            DebugLog.Write(TraceLevel.Info, "Endpoint disconnected: " + recvClient.Client.RemoteEndPoint);

                        OnTcpClientDisconnected(recvClient, session);
                    }
                    catch (Exception e)
                    {
                        if (DebugLog != null)
                            DebugLog.Write(TraceLevel.Error, "HandleIncomingData", e);
                    }
                }
            }
        }

        protected virtual void ProcessIncomingData(RelaySession session, TcpClient recvClient, byte[] buffer, int bytesRead)
        {
            if (DebugLog != null)
                DebugLog.Write(TraceLevel.Verbose, "Received data from: " + recvClient.Client.RemoteEndPoint);

            TcpClient sendClient = null;
            if (recvClient == session.ServerSocket)
                sendClient = session.ClientSocket;
            else if (recvClient == session.ClientSocket)
                sendClient = session.ServerSocket;

            if (sendClient != null)
            {
                SendData(session, recvClient, sendClient, buffer, bytesRead);
            }
            else
            {
                if (DebugLog != null)
                    DebugLog.Write(TraceLevel.Info, "Received data before peer has connected. Data will be stored in backlog and sent once peer connects.");

                session.DataBackLog.Write(buffer, 0, bytesRead);
            }
        }

        protected virtual void OnTcpClientDisconnected(TcpClient tcpClient, RelaySession session)
        {
            // Socket closed
            try { tcpClient.Close(); }
            catch { }

            if (tcpClient == session.ServerSocket)
                KillSession(session);
            else if (tcpClient == session.ClientSocket)
                session.ClientSocket = null;
        }

        protected virtual void OnSendComplete(object state, SocketAsyncEventArgs args)
        {
            try
            {
                object[] sendSession = args.UserToken as object[];
                if (sendSession != null && sendSession.Length >= 3)
                {
                    TcpClient recvClient = sendSession[1] as TcpClient;
                    TcpClient sendClient = sendSession[2] as TcpClient;
                    byte[] buffer = sendSession[3] as byte[];

                    if (DebugLog != null && sendClient != null)
                        DebugLog.Write(TraceLevel.Verbose, "Data sent successfully to: " + sendClient.Client.RemoteEndPoint);

                    // Re-initiate read operation on other socket
                    if (recvClient != null && buffer != null)
                    {
                        object[] recvSession = new[] { sendSession[0], recvClient, buffer };
                        recvClient.Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, HandleIncomingData, recvSession);
                    }
                }
            }
            catch (Exception e)
            {
                if (DebugLog != null)
                    DebugLog.Write(TraceLevel.Error, "OnSendComplete", e);
            }
        }
    }
}
