using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MiracleSticks.PacketRelay
{
    internal class SessionExpirationEnforcer
    {
        private readonly static TimeSpan ExpireTime = TimeSpan.FromSeconds(10);
        private readonly static TimeSpan TimerPeriod = TimeSpan.FromSeconds(10);

        private Timer expireTimer;
        private readonly Dictionary<string, RelaySession> sessions;

        public SessionExpirationEnforcer(Dictionary<string, RelaySession> sessions)
        {
            this.sessions = sessions;
        }

        public void Start()
        {
            expireTimer = new Timer(ExpireSessions, null, TimerPeriod, TimerPeriod);
        }

        public void Stop()
        {
            expireTimer.Dispose();
        }

        private void ExpireSessions(object state)
        {
            lock(sessions)
            {
                DateTime Now = DateTime.Now;

                foreach(var kv in sessions.ToArray())
                {
                    if (kv.Value.ServerIP == null)
                        sessions.Remove(kv.Key);
                    else if (!kv.Value.ServerConnected && ((Now - kv.Value.ServerReserveTime) > ExpireTime))
                        sessions.Remove(kv.Key);
                    else if(kv.Value.ClientIP != null && !kv.Value.ClientConnected && ((Now - kv.Value.ClientReserveTime) > ExpireTime))
                        kv.Value.ClientIP = null;
                }
            }
        }
    }
}
