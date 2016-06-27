using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MiracleSticks.Logging
{
    [Logger(LoggerType.File)]
    public class FileLogger : ILogger
    {
        public const string LogDirName = "Logs";
        private const int MaxNumLogFiles = 100;
        private readonly StreamWriter logStream;
        private readonly object writerLock = new object();
        private TraceLevel minLogLevel = TraceLevel.Verbose;

        public FileLogger()
        {
            DirectoryInfo logDir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            if (logDir.GetDirectories(LogDirName).Length == 0)
                logDir.CreateSubdirectory(LogDirName);
            logDir = logDir.GetDirectories(LogDirName)[0];

            FileInfo newLogFile = new FileInfo(Path.Combine(logDir.FullName, DateTime.Now.ToFileTimeUtc() + ".log"));
            logStream = newLogFile.CreateText();
            WriteLogFileHeader();

            try
            {
                // Delete oldest file when max file threshold is reached
                FileInfo[] logFiles = logDir.GetFiles("*.log");
                if (logFiles.Length >= MaxNumLogFiles)
                {
                    FileInfo oldestFile = newLogFile;
                    foreach(FileInfo logFile in logFiles)
                    {
                        if(logFile.CreationTimeUtc < oldestFile.CreationTimeUtc)
                        {
                            oldestFile = logFile;
                        }
                    }
                    File.Delete(oldestFile.FullName);
                }
            }
            catch (Exception ex)
            {
                Write(TraceLevel.Error, "Failed to delete log file", ex);
            }
        }

        public void Write(TraceLevel level, string message)
        {
            Write(level, message, null);
        }

        public void Write(TraceLevel level, string message, Exception ex)
        {
            if (level > minLogLevel || String.IsNullOrEmpty(message))
                return;

            lock(writerLock)
            {
                logStream.WriteLine("{0} {1} {2}", 
                    DateTime.UtcNow.ToString("yyyy'-'MM'-'dd HH':'mm':'ss"), 
                    GetLevelAbbreviation(level),
                    message);
                
                if(ex != null)
                    logStream.WriteLine(ex.ToString());

                logStream.Flush();
            }
        }

        public void SetMinLogLevel(TraceLevel level)
        {
            minLogLevel = level;
        }

        private void WriteLogFileHeader()
        {
            logStream.WriteLine("Computer Name: " + Environment.MachineName);
            logStream.WriteLine(String.Format("OS: {0} ({1})", Environment.OSVersion.VersionString, Environment.Is64BitOperatingSystem ? "x64" : "x86"));
            logStream.WriteLine("CLR Version: " + Environment.Version);
            logStream.WriteLine();
            logStream.Flush();
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
