using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using MiracleSticks.Logging;
using MiracleSticks.PacketRelay;

namespace MiracleSticks.PacketRelayConsole
{
    class Program
    {
        // Log to console AND file
        private static readonly ILogger log = LoggerFactory.GetLogger(LoggerType.Console, LoggerType.File);

        static void Main(string[] args)
        {
            SessionManager sessionManager = SessionManager.Instance;

            if (args != null && args.Length > 0 && args[0] == "debug")
            {
                sessionManager.DebugLog = log;

                TraceLevel minLogLevel = TraceLevel.Verbose;
                if(args.Length > 1 && Enum.TryParse(args[1], true, out minLogLevel))
                    LoggerFactory.MinLogLevel = minLogLevel;

                Console.WriteLine("-- Debug mode [TraceLevel: {0}] --", minLogLevel);
            }

            sessionManager.Start();

            int serverPort = Convert.ToInt32(ConfigurationManager.AppSettings["RelayApiPort"]);
            if (serverPort <= 0)
                throw new ConfigurationErrorsException("Invalid API port specified in app.config.");

            string serverUrl = "net.tcp://localhost:" + serverPort;
            
            RelayManagement mgmt = new RelayManagement(sessionManager);
            ServiceHost mgmtHost = new ServiceHost(mgmt);
            mgmtHost.AddServiceEndpoint(typeof(IRelayManagement), new NetTcpBinding(), serverUrl);
            mgmtHost.Open();

            Console.WriteLine("-- Packet Relay Started --");
            Console.WriteLine("Press <enter> to stop");
            Console.ReadLine();

            mgmtHost.Close();
            SessionManager.Instance.Stop();
        }
    }
}
