using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MiracleSticks.Logging
{
    [Logger(LoggerType.Console)]
    public class ConsoleLogger : ILogger
    {
        private TraceLevel minLogLevel = TraceLevel.Verbose;

        public void Write(TraceLevel level, string message)
        {
            Write(level, message, null);
        }

        public void Write(TraceLevel level, string message, Exception ex)
        {
            if (level > minLogLevel || String.IsNullOrEmpty(message))
                return;

            Console.WriteLine("{0} {1} {2}", 
                DateTime.UtcNow.ToString("yyyy'-'MM'-'dd HH':'mm':'ss"), 
                GetLevelAbbreviation(level),
                message);

            if (ex != null)
                Console.WriteLine(ex.ToString());
        }

        public void SetMinLogLevel(TraceLevel level)
        {
            minLogLevel = level;
        }

        private static string GetLevelAbbreviation(TraceLevel level)
        {
            switch (level)
            {
                case TraceLevel.Info:
                    return "I";
                case TraceLevel.Warning:
                    return "W";
                case TraceLevel.Error:
                    return "E";
                default:
                    return "V";
            }
        }
    }
}
