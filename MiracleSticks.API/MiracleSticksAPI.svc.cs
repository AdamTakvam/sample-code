using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Data.Entity;
using System.Web;
using MiracleSticks.Logging;
using MiracleSticks.Model;
using System.ServiceModel.Channels;
using MiracleSticks.PacketRelay;

namespace MiracleSticks.API
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = true)]  
    [AspNetCompatibilityRequirements(RequirementsMode=AspNetCompatibilityRequirementsMode.Allowed)]
    public class MiracleSticksAPI : IMiracleSticksAPI
    {        
        private readonly DataContext data;
        private readonly RelayProxy relay;

        public bool RelayConnected { get; private set; }

        public static ILogger DebugLog { get; set; }
        public static bool ForcePingFail { get; set; }

        static MiracleSticksAPI()
        {
            // Just because I'm paranoid doesn't mean they're not out to get me!
            ForcePingFail = false;
        }

        public MiracleSticksAPI()
        {
            string dbConnectionName = ConfigurationManager.AppSettings["DbConnection"];
            if(String.IsNullOrEmpty(dbConnectionName))
                throw new ConfigurationErrorsException("No database connection specified. Add an app config entry \"DbConnection\" which indicates the connection string you want to use.");

            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings[dbConnectionName].ConnectionString);
            data = new DataContext(conn);

            // Attempt to connect to relay
            RelayConnected = false;
            this.relay = new RelayProxy();
            int relayPort = Convert.ToInt32(ConfigurationManager.AppSettings["RelayApiPort"]);
            if (relayPort > 0)
            {
                string relayHost = ConfigurationManager.AppSettings["RelayApiHost"];
                if (String.IsNullOrEmpty(relayHost))
                    relayHost = "localhost";

                RelayConnected = relay.Connect(relayHost, relayPort);
            }
        }

        public RegistrationResponse Register(RegistrationRequest request)
        {
            RegistrationResponse response = new RegistrationResponse();

            if (String.IsNullOrEmpty(request.ComputerName) || 
                String.IsNullOrEmpty(request.GroupID) ||
                String.IsNullOrEmpty(request.StickID))
            {
                response.Success = false;
                response.Description = "Incomplete registration request.";
                return response;
            }

            UserAccount account = null;
                
            try
            {
                account = data.Accounts.Include(acct => acct.Registrations).FirstOrDefault(acct => acct.GroupID == request.GroupID);
            }
            catch(Exception e)
            {
                if (DebugLog != null)
                {
                    while (e.InnerException != null)
                    {
                        DebugLog.Write(TraceLevel.Error, "Database error: " + e.Message);
                        e = e.InnerException;
                    }
                }

                response.Success = false;
                response.Description = "A database error occurred while processing your registration. Please contact technical support.";
                return response;
            }

            if (account != null)
            {
                if(DebugLog != null)
                    DebugLog.Write(TraceLevel.Info, String.Format("Registering endpoint {0}[{1}] for {2}[{3}] {4} relay", request.ComputerName, request.StickID, account.UserName, account.GroupID, request.RequireRelay ? "force" : "no"));

                ServerEndPoint serverEP =
                    account.Registrations.FirstOrDefault(reg => reg.ComputerName == request.ComputerName);

                if (serverEP != null)
                    account.Registrations.Remove(serverEP);

                string sessionId = Guid.NewGuid().ToString();

                if (request.RequireRelay)
                {
                    HalfConnect(sessionId, response);

                    if(response.Success)
                    {
                        account.Registrations.Add(new ServerEndPoint
                                                    {
                                                        StickId = request.StickID,
                                                        SessionId = sessionId,
                                                        ComputerName = request.ComputerName,
                                                        IPAddress = null,
                                                        Port = 0,
                                                        RequireRelay = true,
                                                        RegistrationTime = DateTime.Now
                                                    });
                        data.SaveChanges();
                    }
                }
                else if(request.Port <= 0)
                {
                    response.Success = false;
                    response.Description = "A valid listen port must be specified if relay is not requested.";
                }
                else
                {
                    string clientIP = GetClientIP(OperationContext.Current);
                    if (!String.IsNullOrEmpty(clientIP))
                    {
                        account.Registrations.Add(new ServerEndPoint
                                                        {
                                                            SessionId = sessionId,
                                                            ComputerName = request.ComputerName,
                                                            IPAddress = clientIP,
                                                            Port = request.Port,
                                                            RequireRelay = false,
                                                            RegistrationTime = DateTime.Now
                                                        });
                        data.SaveChanges();
                        response.Success = true;
                    }
                    else
                    {
                        response.Description = "Could not determine source IP address.";
                    }
                }
            }
            else
            {
                response.Success = false;
                response.Description = "Group ID not found.";
            }

            return response;
        }

        public UnregisterResponse Unregister(UnregisterRequest request)
        {
            UnregisterResponse response = new UnregisterResponse { Success = false };
            UserAccount account = null;
            
            try
            {
                account = data.Accounts.Include(acct => acct.Registrations).FirstOrDefault(acct => acct.GroupID == request.GroupID);
            }
            catch(Exception e)
            {
                if (DebugLog != null)
                {
                    while (e.InnerException != null)
                    {
                        DebugLog.Write(TraceLevel.Error, "Database error: " + e.Message);
                        e = e.InnerException;
                    }
                }
                return response;
            }

            if(account != null)
            {
                if (DebugLog != null)
                    DebugLog.Write(TraceLevel.Info, String.Format("Unregistering endpoint {0} for {1}[{2}]", request.ComputerName, account.UserName, account.GroupID));

                foreach (ServerEndPoint serverEP in account.Registrations.Where(x => x.ComputerName == request.ComputerName).ToArray())
                {
                    if (serverEP != null)
                    {
                        account.Registrations.Remove(serverEP);
                        response.Success = true;
                    }
                }

                if(response.Success)
                    data.SaveChanges();
            }

            return response;
        }

        public QueryResponse Query(QueryRequest request)
        {
            QueryResponse response = new QueryResponse();
            UserAccount account = null;
            
            try
            {
                account = data.Accounts.Include(acct => acct.Registrations).FirstOrDefault(acct => acct.GroupID == request.GroupID);
            }
            catch (Exception e)
            {
                if (DebugLog != null)
                {
                    while (e.InnerException != null)
                    {
                        DebugLog.Write(TraceLevel.Error, "Database error: " + e.Message);
                        e = e.InnerException;
                    }
                }

                response.Success = false;
                response.Description = "A database error occurred while processing your request. Please contact technical support.";
                return response;
            }
          
            if(account != null && account.Registrations != null)
            {
                response.Success = true;

                foreach(ServerEndPoint serverEP in account.Registrations)
                {
                    response.Servers.Add(new ServerRegistration
                    {
                        SessionId = serverEP.SessionId,
                        ComputerName = serverEP.ComputerName, 
                        IPAddress = serverEP.IPAddress, 
                        Port = serverEP.Port
                    });
                }
            }
            else
            {
                response.Success = false;
                response.Description = "GroupID not found.";
            }

            return response;
        }

        private void HalfConnect(string sessionId, RegistrationResponse response)
        {
            if(response == null)
                throw new ArgumentNullException("response");

            response.Success = false;

            string clientIP = GetClientIP(OperationContext.Current);
            if (String.IsNullOrEmpty(clientIP))
            {
                response.Description = "Could not determine source IP address.";
            }
            else
            {
                // Send half-connect to relay
                if (RelayConnected)
                {
                    var connectResponse = relay.Proxy.HalfConnect(sessionId, clientIP);
                    if (connectResponse.Result == RelayConnectResponse.ResultCode.Success)
                    {
                        response.Success = true;
                        response.RelayIPAddress = connectResponse.IPAddress;
                        response.RelayPort = connectResponse.Port;
                    }
                    else
                    {
                        response.Description = connectResponse.Description;

                        if(DebugLog != null)
                            DebugLog.Write(TraceLevel.Warning, "Relay half-connect failed: " + response.Description);
                    }
                }
                else
                {
                    response.Description = "No relay services are available. Contact support for assistance.";
                }
            }
        }

        private static string GetClientIP(OperationContext context)
        {
            string clientIP = null;

            if (context != null &&
                context.IncomingMessageProperties != null)
            {
                var remoteEP = context.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                if (remoteEP != null)
                    clientIP = remoteEP.Address;
            }
            
            return clientIP;
        }

        public ConnectResponse Connect(ConnectRequest request)
        {
            var response = new ConnectResponse { Success = false };

            string clientIP = GetClientIP(OperationContext.Current);
            if(String.IsNullOrEmpty(clientIP))
            {
                response.Description = "Could not determine source IP address.";
            }
            else
            {
                // Send full-connect to relay
                if (RelayConnected)
                {
                    var connectResponse = relay.Proxy.FullConnect(request.SessionId, clientIP);
                    if(connectResponse.Result == RelayConnectResponse.ResultCode.Success)
                    {
                        response.Success = true;
                        response.RelayIPAddress = connectResponse.IPAddress;
                        response.RelayPort = connectResponse.Port;
                    }
                    else if(connectResponse.Result == RelayConnectResponse.ResultCode.UnknownSession)
                    {
                        response.Description = connectResponse.Description;
                        
                        try
                        {
                            // Nuke the stale registration
                            UserAccount account = data.Accounts.Include(acct => acct.Registrations).FirstOrDefault(acct => acct.GroupID == request.GroupID);
                            if (account != null && account.Registrations != null)
                            {
                                var staleRegistration = account.Registrations.FirstOrDefault(reg => reg.SessionId == request.SessionId);
                                if (staleRegistration != null)
                                {
                                    account.Registrations.Remove(staleRegistration);
                                    data.SaveChanges();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (DebugLog != null)
                            {
                                while (e.InnerException != null)
                                {
                                    DebugLog.Write(TraceLevel.Error, "Database error: " + e.Message);
                                    e = e.InnerException;
                                }
                            }
                        }
                    }
                    else
                    {
                        response.Description = connectResponse.Description;

                        if (DebugLog != null)
                            DebugLog.Write(TraceLevel.Warning, "Relay full-connect failed: " + response.Description);
                    }
                }
                else
                {
                    response.Description = "No relay services are available. Contact support for assistance.";
                }
            }

            return response;
        }

        public PortTestResponse PortTest(PortTestRequest request)
        {
            PortTestResponse response = new PortTestResponse();
            string remoteIP = GetClientIP(OperationContext.Current);

            if (ForcePingFail)
            {
                response.Success = false;
            }
            else
            {
                try
                {
                    TcpClient client = new TcpClient();
                    client.Connect(remoteIP, request.Port);
                    response.Success = client.Connected;
                    client.Close();
                }
                catch
                {
                    response.Success = false;
                }
            }

            if (DebugLog != null)
                DebugLog.Write(TraceLevel.Verbose, String.Format("Port test to {0}:{1} {2}.", remoteIP, request.Port, response.Success ? "succeeded" : "failed"));

            return response;
        }
    }
}
