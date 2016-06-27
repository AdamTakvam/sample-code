using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using NATUPNPLib;

namespace MiracleSticksServer.Net
{
    public class UPnP
    {
        private const int MaxPort = 65535;

        /// <summary>Indicates if a UPnP-enabled NAT device is found on the local network.</summary>
        public static bool NatDeviceFound
        {
            get
            {
                UPnPNAT upnpnat = new UPnPNAT();
                return upnpnat.StaticPortMappingCollection != null;
            }
        }

        /// <summary>Returns the external endpoint mapped to the specified port on this computer.</summary>
        public static IPEndPoint GetExternalEndpoint(int localPort)
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

            UPnPNAT upnpnat = new UPnPNAT();
            if (upnpnat.StaticPortMappingCollection != null)
            {
                foreach (IStaticPortMapping mapping in upnpnat.StaticPortMappingCollection)
                {
                    IPAddress localMapAddress = IPAddress.Parse(mapping.InternalClient);
                    if (localIPs.Contains(localMapAddress) && mapping.InternalPort == localPort)
                        return new IPEndPoint(IPAddress.Parse(mapping.ExternalIPAddress), mapping.ExternalPort);
                }
            }
            return null;
        }

        /// <summary>Attempts to create a map to the specified port on the local machine on the next highest available external port to the one specified</summary>
        /// <returns>The actual external port mapped</returns>
        public static IPEndPoint MapPort(int localPort, int externalPortStart)
        {
            if(localPort > MaxPort)
                throw new ArgumentOutOfRangeException("localPort");
            if(externalPortStart > MaxPort)
                throw new ArgumentOutOfRangeException("externalPortStart");

            UPnPNAT upnpnat = new UPnPNAT();
            if (upnpnat.StaticPortMappingCollection != null)
            {
                DeleteTeredoMappings(upnpnat.StaticPortMappingCollection);

                int externalPort = externalPortStart;

                bool externalPortInUse;
                do
                {
                    externalPortInUse = false;
                    foreach (IStaticPortMapping mapping in upnpnat.StaticPortMappingCollection)
                    {
                        if (mapping.ExternalPort == externalPort)
                        {
                            externalPortInUse = true;
                            externalPort++;
                            break;
                        }
                    }
                } while (externalPortInUse);

                if (externalPort > MaxPort)
                    throw new UPnPFailedException("All ports >= " + externalPortStart + " are in use");

                IPAddress routedInterface = NetworkAdapters.GetRoutedInterface();
                if(routedInterface == null)
                    throw new UPnPFailedException("Could not determine routed interface");

                upnpnat.StaticPortMappingCollection.Add(externalPort, "TCP", localPort, routedInterface.ToString(), true, "MiracleSticks Server");
            }
            else
            {
                throw new UPnPFailedException("No UPnP-enabled device found");
            }

            return GetExternalEndpoint(localPort);
        }

        public static void RemoveMapping(int externalPort)
        {
             UPnPNAT upnpnat = new UPnPNAT();
             if (upnpnat.StaticPortMappingCollection != null)
             {
                 upnpnat.StaticPortMappingCollection.Remove(externalPort, "TCP");
             }
        }

        /// <summary>Removes virus-like "Teredo" mappings which Windows 7 tends to fill up the map table with.</summary>
        /// <remarks>If you don't do this, the UPnP mapping table will always be full and the attempted mapping will always fail.</remarks>
        private static void DeleteTeredoMappings(IStaticPortMappingCollection mappings)
        {
            if (mappings == null)
                return;

            List<IStaticPortMapping> teredoMappings = new List<IStaticPortMapping>();
            foreach (IStaticPortMapping mapping in mappings)
            {
                if (mapping.Description.ToLower().Contains("teredo"))
                    teredoMappings.Add(mapping);
            }

            foreach(IStaticPortMapping mapping in teredoMappings)
            {
                mappings.Remove(mapping.ExternalPort, mapping.Protocol);
            }
        }
    }

    public class UPnPFailedException : Exception
    {
        public UPnPFailedException(string message)
            : base(message)
        {
        }
    }
}
