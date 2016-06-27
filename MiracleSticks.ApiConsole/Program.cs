using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Web;
using System.Configuration;
using System.Text;
using MiracleSticks.API;
using System.ServiceModel;
using MiracleSticks.Logging;

namespace MiracleSticks.ApiConsole
{
    class Program
    {
        // Log to console AND file
        private static readonly ILogger log = LoggerFactory.GetLogger(LoggerType.Console, LoggerType.File);

        static void Main(string[] args)
        {
            string apiHostUri = ConfigurationManager.AppSettings["ApiHostUri"];
            if (String.IsNullOrEmpty(apiHostUri))
            {
                Console.WriteLine("Error: No API host URI configured");
                return;
            }

            if (args != null && args.Length > 0 && (args[0] == "debug" || args[0] == "pingfail"))
            {
                bool pingFail = String.Compare(args[0], "pingfail", true) == 0;
                bool debug = pingFail || String.Compare(args[0], "debug", true) == 0;

                if (debug)
                {
                    MiracleSticksAPI.DebugLog = log;

                    TraceLevel minLogLevel = TraceLevel.Verbose;
                    if (args.Length > 1 && Enum.TryParse(args[1], true, out minLogLevel))
                        LoggerFactory.MinLogLevel = minLogLevel;

                    Console.WriteLine("-- Debug mode [TraceLevel: {0}] --", minLogLevel);

                    if(pingFail)
                    {
                        MiracleSticksAPI.ForcePingFail = true;
                        Console.WriteLine("WARNING: PING FAIL MODE IS ACTIVE -- Debug only!");
                    }
                }
            }

            Uri baseAddress = new Uri(apiHostUri);

            ServiceHost host = new ServiceHost(typeof(MiracleSticksAPI));
            host.AddServiceEndpoint(typeof(IMiracleSticksAPI), new BasicHttpContextBinding(), baseAddress);
            host.Open();

            Console.WriteLine("MiracleSticks Web API Running on {0}", apiHostUri);
            Console.WriteLine("Press <enter> to stop");
            Console.ReadLine();

            host.Close();
        }
    }
}
