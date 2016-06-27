using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;

namespace MiracleSticks.PacketRelay
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class RelayManagement : IRelayManagement
    {
        private readonly SessionManager sessionManager;

        public RelayManagement()
            : this(SessionManager.Instance) { }

        public RelayManagement(SessionManager sessionManager)
        {
            if(sessionManager == null)
                throw new ArgumentNullException("sessionManager");

            this.sessionManager = sessionManager;
        }

        public RelayConnectResponse HalfConnect(string sessionId, string serverIP)
        {
            var response = new RelayConnectResponse { Result = RelayConnectResponse.ResultCode.Failure };

            if (String.IsNullOrEmpty(sessionId) || String.IsNullOrEmpty(serverIP))
            {
                response.Description = "Invalid relay connection parameters";
            }
            else
            {
                IPAddress serverIPAddr;
                if (!IPAddress.TryParse(serverIP, out serverIPAddr))
                {
                    response.Description = "Server IP address is not valid.";
                }
                else
                {
                    try
                    {
                        sessionManager.CreateSession(sessionId, serverIPAddr);
                        response.Result = RelayConnectResponse.ResultCode.Success;
                        response.IPAddress = sessionManager.ExternalHostname;
                        response.Port = sessionManager.RelayEP.Port;
                    }
                    catch (Exception e)
                    {
                        // Never throw exceptions over an IPC connection
                        response.Description = e.Message;
                    }
                }
            }

            return response;
        }

        public RelayConnectResponse FullConnect(string sessionId, string clientIP)
        {
            var response = new RelayConnectResponse { Result = RelayConnectResponse.ResultCode.Failure };

            if (String.IsNullOrEmpty(sessionId) || String.IsNullOrEmpty(clientIP))
            {
                response.Description = "Invalid relay connection parameters";
            }
            else
            {
                IPAddress clientIPAddr;
                if (!IPAddress.TryParse(clientIP, out clientIPAddr))
                {
                    response.Description = "Client IP address is not valid.";
                }
                else
                {
                    try
                    {
                        sessionManager.ConnectSession(sessionId, clientIPAddr);
                        response.Result = RelayConnectResponse.ResultCode.Success;
                        response.IPAddress = sessionManager.ExternalHostname;
                        response.Port = sessionManager.RelayEP.Port;
                    }
                    catch(SessionNotFoundException e)
                    {
                        response.Result = RelayConnectResponse.ResultCode.UnknownSession;
                        response.Description = e.Message;
                    }
                    catch (Exception e)
                    {
                        // Never throw exceptions over an IPC connection
                        response.Description = e.Message;
                    }
                }
            }

            return response;
        }

        public bool Ping()
        {
            return true;
        }
    }
}
