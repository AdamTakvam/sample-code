using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web;
using MiracleSticks.PacketRelay;

namespace MiracleSticks.API
{
    internal class RelayProxy : IDisposable
    {
        private IRelayManagement proxy = null;

        public IRelayManagement Proxy { get { return proxy; } }

        public bool Connect(string host, int port)
        {
            if(proxy != null)
                ((ICommunicationObject)proxy).Close();

            string serverUrl = String.Format("net.tcp://{0}:{1}", host, port);
            var relayEP = new EndpointAddress(serverUrl);
            var binding = new NetTcpBinding();
            var channelFactory = new ChannelFactory<IRelayManagement>(binding, relayEP);

            try
            {
                proxy = channelFactory.CreateChannel();
                proxy.Ping();
                return true;
            }
            catch
            {
                if (proxy != null)
                {
                    ((ICommunicationObject)proxy).Abort();
                    proxy = null;
                }
                return false;
            }
        }

        public void Dispose()
        {
            if (proxy != null)
            {
                ((ICommunicationObject) proxy).Close();
                proxy = null;
            }
        }
    }
}