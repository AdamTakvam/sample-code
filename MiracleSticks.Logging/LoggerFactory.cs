using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MiracleSticks.Logging
{
    public class LoggerFactory
    {
        private static readonly Dictionary<LoggerType, ILogger> loggers = new Dictionary<LoggerType, ILogger>();
        private static TraceLevel minLogLevel = TraceLevel.Verbose;

        public static TraceLevel MinLogLevel
        {
            get { return minLogLevel; }
            set
            {
                minLogLevel = value;
                foreach(ILogger logger in loggers.Values)
                {
                    logger.SetMinLogLevel(minLogLevel);
                }
            }
        }

        public static ILogger GetLogger(params LoggerType[] types)
        {
            List<ILogger> logSinks = new List<ILogger>();

            foreach (LoggerType type in types)
            {
                if (!loggers.ContainsKey(type))
                {
                    ILogger logger = CreateLogger(type);
                    if (logger == null) throw new LoggerException("No log sink found for type: " + type);
                    logger.SetMinLogLevel(minLogLevel);

                    loggers.Add(type, logger);
                }
                logSinks.Add(loggers[type]);
            }
            return new LoggerFacade(logSinks);
        }

        private static ILogger CreateLogger(LoggerType type)
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            foreach(Type t in thisAssembly.GetTypes())
            {
                if (t.IsClass && typeof(ILogger).IsAssignableFrom(t))
                {
                    foreach(object attrObj in t.GetCustomAttributes(typeof(LoggerAttribute), false))
                    {
                        LoggerAttribute attr = attrObj as LoggerAttribute;
                        if (attr != null && attr.LoggerType == type)
                            return Activator.CreateInstance(t) as ILogger;
                    }
                }
            }
            return null;
        }
    }

    public class LoggerException : Exception
    {
        public LoggerException(string message)
            : base(message)
        {
        }
    }
}
