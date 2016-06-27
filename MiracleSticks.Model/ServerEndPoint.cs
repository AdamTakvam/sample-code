using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

namespace MiracleSticks.Model
{
    public class ServerEndPoint
    {
        public int Id { get; set; }

        public string StickId { get; set; }

        public string SessionId { get; set; }

        public string ComputerName { get; set; }

        public string IPAddress { get; set; }

        public int Port { get; set; }

        public bool RequireRelay { get; set; }
        
        public DateTime RegistrationTime { get; set; }
    }
}
