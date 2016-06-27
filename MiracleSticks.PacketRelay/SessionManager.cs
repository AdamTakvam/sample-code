using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MiracleSticks.Logging;

namespace MiracleSticks.PacketRelay
{
    public class SessionManager
    {
        #region Singleton

        private static SessionManager instance;
        private static readonly object instanceLock = new object();

        public static SessionManager Instance
        {
            get
            {
                lock(instanceLock)
                {
                    if (instance == null)
                    {
                        int listenPort = Convert.ToInt32(ConfigurationManager.AppSettings["RelayListenPort"]);
                        string externalHostname = ConfigurationManager.AppSettings["ExternalHostname"];
                        instance = new SessionManager(listenPort, externalHostname);
                    }
                    return instance;
                }
            }
        }
        #endregion

        public const string ClientConnectedMsg = "--ClientConnected--";
        public const string ClientDisconnectedMsg = "--ClientDisconnected--";

        private readonly Dictionary<string, RelaySession> sessions;
        private readonly TcpListener tcpListener;
        private readonly PersistentServerRelay relay; 
        private readonly SessionExpirationEnforcer expirationEnforcer;

        public IPEndPoint RelayEP { get { return tcpListener.LocalEndpoint as IPEndPoint; } }
        public string ExternalHostname { get; private set; }
        
        private ILogger _debugLog;
        public ILogger DebugLog
        {
            get { return _debugLog; }
            set 
            {
                _debugLog = value;
                relay.DebugLog = value;
            }
        }

        private SessionManager(int listenPort, string externalHostname)
        {
            if(listenPort <= 0)
                throw new ConfigurationErrorsException("Invalid listen port");
            if(String.IsNullOrEmpty(externalHostname))
                throw new ConfigurationErrorsException("Invalid external hostname");

            this.ExternalHostname = externalHostname;
            this.sessions = new Dictionary<string, RelaySession>();
            this.expirationEnforcer = new SessionExpirationEnforcer(sessions);
            this.relay = new PersistentServerRelay(sessions);
            this.tcpListener = new TcpListener(IPAddress.Any, listenPort);
        }

        public void Start()
        {
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(HandleIncomingConnection, null);
            expirationEnforcer.Start();
        }

        public void Stop()
        {
            try { tcpListener.Stop(); }
            catch {}
            
            try { expirationEnforcer.Stop(); }
            catch {}

            try { relay.Dispose(); }
            catch {}
        }

        public void CreateSession(string sessionId, IPAddress serverIP)
        {
            lock (sessions)
            {
                RelaySession session;
                if (sessions.TryGetValue(sessionId, out session))
                {
                    relay.KillSession(session);
                    sessions.Remove(sessionId);
                }

                session = new RelaySession(sessionId)
                {
                    ServerIP = serverIP,
                    ServerReserveTime = DateTime.Now,
                };

                sessions[sessionId] = session;

                if(DebugLog != null)
                {
                    DebugLog.Write(TraceLevel.Info, String.Format("New session created: {0} ({1})", sessionId, serverIP));
                    DebugLog.Write(TraceLevel.Verbose, FormatSessionTable(sessions));
                }
            }
        }

        public void ConnectSession(string sessionId, IPAddress clientIP)
        {
            lock (sessions)
            {
                RelaySession session = null;
                if(!sessions.TryGetValue(sessionId, out session))
                    throw new SessionNotFoundException(sessionId);

                if(!session.ServerConnected)
                    throw new RelayConnectException(sessionId, "Server not connected.");

                session.ClientIP = clientIP;
                session.ClientReserveTime = DateTime.Now;

                if (DebugLog != null)
                {
                    DebugLog.Write(TraceLevel.Info, String.Format("Session connected: {0} ({1})", sessionId, clientIP));
                    DebugLog.Write(TraceLevel.Verbose, FormatSessionTable(sessions));
                }
            }
        }

        private void HandleIncomingConnection(IAsyncResult ar)
        {
            TcpClient newClient = tcpListener.EndAcceptTcpClient(ar);
            if (newClient.Connected)
            {
                lock (sessions)
                {
                    IPAddress remoteIP = ((IPEndPoint) newClient.Client.RemoteEndPoint).Address;
                    RelaySession session = null;

                    foreach (var s in sessions.Values)
                    {
                        if (s.ServerIP != null && s.ServerIP.Equals(remoteIP) && !s.ServerConnected)
                        {
                            session = s;
                            session.ServerSocket = newClient;

                            if (DebugLog != null)
                            {
                                DebugLog.Write(TraceLevel.Info, String.Format("Server connected: {0}", remoteIP));
                                DebugLog.Write(TraceLevel.Verbose, FormatSessionTable(sessions));
                            }

                            relay.AddClient(session, newClient);
                            break;
                        }

                        if (s.ClientIP != null && s.ClientIP.Equals(remoteIP) && s.ServerConnected && !s.ClientConnected)
                        {
                            session = s;
                            session.ClientSocket = newClient;

                            if (DebugLog != null)
                            {
                                DebugLog.Write(TraceLevel.Info, String.Format("Client connected: {0}", remoteIP));
                                DebugLog.Write(TraceLevel.Verbose, FormatSessionTable(sessions));
                            }

                            // Tell server that a client has connected
                            byte[] buffer = Encoding.UTF8.GetBytes(ClientConnectedMsg);
                            relay.SendData(session, session.ClientSocket, session.ServerSocket, buffer, buffer.Length);
                            break;
                        }
                    }

                    if (session == null)
                        newClient.Close();
                }
            }

            tcpListener.BeginAcceptTcpClient(HandleIncomingConnection, null);
        }

        private static string FormatSessionTable(Dictionary<string, RelaySession> sessions)
        {
            if (sessions == null || sessions.Count == 0)
                return "<empty>";

            string format = String.Empty;
            foreach(var kv in sessions)
            {
                format += String.Format("{0}: S={1}({2}) C={3}({4})\n", 
                    kv.Key,
                    kv.Value.ServerIP == null ? "null" : kv.Value.ServerIP.ToString(), 
                    kv.Value.ServerConnected ? "X" : " ",
                    kv.Value.ClientIP == null ? "null" : kv.Value.ClientIP.ToString(), 
                    kv.Value.ClientConnected ? "X" : " ");
            }
            return format;
        }
    }
}
