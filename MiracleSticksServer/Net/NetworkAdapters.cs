using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MiracleSticksServer.Net
{
    public class NetworkAdapters
    {
        public static IPAddress GetRoutedInterface()
        {
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
                IPAddress gateway = IPAddress.Parse(GetInternetGateway());
                return FindMatch(addresses, gateway);
            }
            catch (FormatException) { return null; }
        }

        // Dirty... feel free to find a better way.
        private static string GetInternetGateway()
        {
            using (Process tracert = new Process())
            {
                tracert.StartInfo.FileName = "tracert.exe";
                tracert.StartInfo.Arguments = "-h 1 www.google.com";
                tracert.StartInfo.UseShellExecute = false;
                tracert.StartInfo.RedirectStandardOutput = true;
                tracert.StartInfo.CreateNoWindow = true;
                tracert.Start();

                using (StreamReader reader = tracert.StandardOutput)
                {
                    string line;
                    while((line = reader.ReadLine()) != null)
                    {
                        string gwAddr = ParseTraceRouteOutput(line);
                        if (gwAddr != null)
                        {
                            tracert.Kill();
                            return gwAddr;
                        }
                    }
                    return null;
                }
            }
        }

        private static string ParseTraceRouteOutput(string line)
        {
            if (String.IsNullOrEmpty(line))
                return null;

            line = line.Trim();
            if (line[0] == '1')
            {
                if (line[line.Length - 1] == ']')
                {
                    int index = line.LastIndexOf('[');
                    if (index > 0)
                        return line.Substring(index + 1, line.Length - (index + 2));
                }
                else
                {
                    return line.Substring(line.LastIndexOf(' ') + 1);
                }
            }
            return null;
        }

        // Only IPv4
        private static IPAddress FindMatch(IPAddress[] addresses, IPAddress gateway)
        {
            byte[] gatewayBytes = gateway.GetAddressBytes();
            foreach (IPAddress ip in addresses)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    byte[] ipBytes = ip.GetAddressBytes();
                    if (ipBytes[0] == gatewayBytes[0]
                        && (ipBytes[0] < 127 || ipBytes[1] == gatewayBytes[1])
                        && (ipBytes[0] < 192 || ipBytes[2] == gatewayBytes[2]))
                    {
                        return ip;
                    }
                }
            }
            return null;
        }
    }
}
