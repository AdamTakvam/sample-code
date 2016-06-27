using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MiracleSticks.Logging
{
    public class LoggerFacade : ILogger
    {
        private readonly List<ILogger> logSinks;

        public LoggerFacade(IEnumerable<ILogger> sinks)
        {
            this.logSinks = sinks.ToList();
        }

        public void Write(TraceLevel level, string message)
        {
            foreach(ILogger sink in logSinks)
            {
                sink.Write(level, message);
            }
        }

        public void Write(TraceLevel level, string message, Exception ex)
        {
            foreach (ILogger sink in logSinks)
            {
                sink.Write(level, message, ex);
            }
        }

        public void SetMinLogLevel(TraceLevel level)
        {
            foreach (ILogger sink in logSinks)
            {
                sink.SetMinLogLevel(level);
            }
        }
    }
}
