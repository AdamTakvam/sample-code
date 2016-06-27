using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;

namespace MiracleSticks.Configuration
{
    public class ConfigData
    {
        public string StickId { get; set; }

        public string GroupId { get; set; }

        public string ComputerName { get; set; }

        public int Port { get; set; }

        public string ServerPassword { get; set; }

        public string Signature { get; set; }
    }

    /// <summary>I'm a stickler for data object purity.</summary>
    public static class ConfigDataEx
    {
        public static bool IsPasswordSet(this ConfigData configData)
        {
            return !String.IsNullOrWhiteSpace(configData.ServerPassword);
        }
    }
}
