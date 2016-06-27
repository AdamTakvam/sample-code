using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using MiracleSticks.PacketRelay;

namespace MiracleSticks.PacketRelayServiceHost
{
    public partial class PacketRelayServiceHost : ServiceBase
    {
        private ServiceHost mgmtHost;

        public PacketRelayServiceHost()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            SessionManager.Instance.Start();

            if (mgmtHost != null)
                Stop();

            int serverPort = Convert.ToInt32(ConfigurationManager.AppSettings["RelayApiPort"]);
            if(serverPort <= 0)
                throw new ConfigurationErrorsException("Invalid API port specified in app.config.");

            string serverUrl = "net.tcp://localhost:" + serverPort;

            mgmtHost = new ServiceHost(typeof(RelayManagement));
            mgmtHost.AddServiceEndpoint(typeof(IRelayManagement), new NetTcpBinding(), serverUrl);
            mgmtHost.Open();
        }

        protected override void OnStop()
        {
            mgmtHost.Close();
            mgmtHost = null;

            SessionManager.Instance.Stop();
        }
    }
}
