using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MiracleSticks.Logging
{
    public enum LoggerType
    {
        Console,
        File
    }

    public class LoggerAttribute : Attribute
    {
        public LoggerType LoggerType { get; private set; }

        public LoggerAttribute(LoggerType type)
        {
            LoggerType = type;
        }
    }

    public interface ILogger
    {
        void SetMinLogLevel(TraceLevel level);
        void Write(TraceLevel level, string message);
        void Write(TraceLevel level, string message, Exception ex);
    }
}
