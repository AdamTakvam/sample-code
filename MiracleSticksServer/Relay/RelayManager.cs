using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MiracleSticks.Logging;
using MiracleSticks.PacketRelay;

namespace MiracleSticksServer.Relay
{
    public class RelayManager
    {
        private readonly ILogger log = LoggerFactory.GetLogger(LoggerType.File);

        private int _localPort;
        private Thread relayThread;
        private RelaySession session;
        private DelayedConnectRelay socketRelay;
        private readonly AutoResetEvent relayStarted = new AutoResetEvent(false);
        
        public string ErrorDescription { get; private set; }

        public bool Start(IPEndPoint remoteEP, int localPort)
        {
            if(localPort <= 0)
                throw new ArgumentException("localport must be greater than zero");
            if(remoteEP == null)
                throw new ArgumentNullException("remoteEP");

            _localPort = localPort;
            relayThread = new Thread(RelayThread);
            relayThread.Start(new object[] { remoteEP, localPort });

            relayStarted.WaitOne();
            return ErrorDescription == null;
        }

        public void Stop()
        {
            if (socketRelay != null)
            {
                socketRelay.KillSession(session);
                socketRelay = null;
            }
        }

        private void RelayThread(object state)
        {
            TcpClient extConn = new TcpClient();

            try
            {
                object[] args = state as object[];
                if (args == null)
                {
                    log.Write(TraceLevel.Error, "CODING ERROR: RelayThread started with invalid arguments!");
                    ErrorDescription = "Internal error. Please contact MiracleSticks technical support.";
                    return;
                }

                IPEndPoint remoteEP = args[0] as IPEndPoint;

                if(remoteEP == null)
                {
                    log.Write(TraceLevel.Error, "CODING ERROR: RelayThread started with invalid arguments!");
                    ErrorDescription = "Internal error. Please contact MiracleSticks technical support.";
                    return;
                }
                
                try
                {
                    extConn.Connect(remoteEP);
                }
                catch(Exception ex)
                {
                    log.Write(TraceLevel.Error, String.Format("Failed to connect to relay: {0}:{1}", remoteEP.Address, remoteEP.Port), ex);
                    ErrorDescription = "Failed to connect to MiracleSticks relay service";
                    return;
                }

                session = new RelaySession(Guid.NewGuid().ToString())
                            {
                                ServerIP = remoteEP.Address,
                                ClientIP = IPAddress.Loopback,
                                ClientReserveTime = DateTime.Now,
                                ClientSocket = null,
                                ServerReserveTime = DateTime.Now,
                                ServerSocket = extConn
                            };

                Dictionary<string, RelaySession> sessions = new Dictionary<string, RelaySession>();
                sessions.Add(session.SessionId, session);

                socketRelay = new DelayedConnectRelay(sessions);
                socketRelay.ClientConnected += OnClientConnected;
                socketRelay.ClientDisconnected += OnClientDisconnected;
                
                if(AppMain.DebugMode)
                    socketRelay.DebugLog = log;

                socketRelay.AddClient(session, extConn);
            }
            catch(Exception ex)
            {
                log.Write(TraceLevel.Error, "Relay connection error", ex);
                ErrorDescription = "Relay connection error: " + ex.Message;
            }
            finally
            {
                relayStarted.Set();
            }
        }

        private void OnClientConnected()
        {
            log.Write(TraceLevel.Info, "Remote client connected. Establishing localhost connection to VNC server.");

            try
            {
                session.ClientSocket = new TcpClient();
                session.ClientSocket.Connect(IPAddress.Loopback, _localPort);
                socketRelay.AddClient(session, session.ClientSocket);
            }
            catch (Exception ex)
            {
                log.Write(TraceLevel.Error, String.Format("Failed to connect to VNC server: localhost:{0}", _localPort), ex);
                ErrorDescription = "Failed to connect to MiracleSticks relay service";
                return;
            }
        }

        private void OnClientDisconnected()
        {
            log.Write(TraceLevel.Info, "Remote client disconnected. Closing localhost connection to VNC server.");

            if (session.ClientSocket != null)
            {
                try { session.ClientSocket.Close(); } catch { }
                session.ClientSocket = null;
            }
        }
    }
}
