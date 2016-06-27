using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Web;
using System.ServiceProcess;
using System.Text;
using MiracleSticks.API;

namespace MiracleSticks.ServiceHost
{
    public partial class APIServiceHost : ServiceBase
    {
        System.ServiceModel.ServiceHost host;

        public APIServiceHost()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string apiHostUri = ConfigurationManager.AppSettings["ApiHostUri"];
            Uri baseAddress = new Uri(apiHostUri);

            MiracleSticksAPI msAPI = new MiracleSticksAPI();
            host = new System.ServiceModel.ServiceHost(msAPI, baseAddress);
            host.Open();
        }

        protected override void OnStop()
        {
            if(host != null)
                host.Close();
        }
    }
}
