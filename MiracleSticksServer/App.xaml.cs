using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using MiracleSticks.Configuration;
using MiracleSticks.Logging;
using MiracleSticks.Utilities;

namespace MiracleSticksServer
{
    public enum AppState
    {
        Initializing,
        ConfiguringFirewall,
        Registered
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class AppMain : Application
    {
        private readonly ILogger log = LoggerFactory.GetLogger(LoggerType.File);

        public const string DisplayName = "MiracleSticks Server";

        public static AppState State { get; set; }
        public static MainServerWindow ProgressWindow { get; private set; }
        public static bool DebugMode { get; private set; }

        protected void RunApplication(object sender, StartupEventArgs e)
        {
            State = AppState.Initializing;
            CommandLineArguments args = new CommandLineArguments(e.Args);

            if (args.IsParamPresent("showconfig"))
            {
                MessageBox.Show(String.Format("Stick ID: {0}\nGroup ID: {1}\nComputer Name: {2}\nVNC Password Set? {3}",
                    ConfigManager.Data.StickId, ConfigManager.Data.GroupId, ConfigManager.Data.ComputerName, ConfigManager.Data.IsPasswordSet() ? "Yes" : "No"),
                    "Stick Configuration", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown(0);
                return;
            }

            bool staging = false;
            string groupId = args.GetSingleParam("groupid");
            string stickId = args.GetSingleParam("stickid");
            if (!String.IsNullOrEmpty(groupId))
            {
                staging = true;
                ConfigManager.Data.GroupId = groupId;
            }

            if (!String.IsNullOrEmpty(stickId))
            {
                staging = true;
                ConfigManager.Data.StickId = stickId;
            }

            if (staging)
            {
                ConfigManager.Save();
                Shutdown(0);
                return;
            }

            DebugMode = args.IsParamPresent("debug");
            if (DebugMode)
                log.Write(TraceLevel.Info, "-- Running in debug mode --");

            if (String.IsNullOrEmpty(ConfigManager.Data.GroupId))
            {
                MessageBox.Show("USB key is corrupt: No Group Id is set", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            if (String.IsNullOrEmpty(ConfigManager.Data.StickId))
            {
                MessageBox.Show("USB key is corrupt: No Stick Id is set", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            // Validate config data
            if(ConfigManager.Data.Signature != ConfigManager.ComputeSignature(ConfigManager.Data))
            {
                MessageBox.Show("USB key is corrupt: Config file is invalid", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            EnsureSingleProcess();

            ProgressWindow = new MainServerWindow();
            ProgressWindow.ShowDialog();
        }

        // VNC threads can cause the app to hang around after the main window is closed.
        // Ensure there are no duplicate instances.
        private static void EnsureSingleProcess()
        {
            Process thisProcess = Process.GetCurrentProcess();
            foreach (Process process in Process.GetProcessesByName(thisProcess.ProcessName))
            {
                if (process.Id != thisProcess.Id)
                    process.Kill();
            }
        }

        public static void Shutdown(bool fatalError)
        {
            ProgressWindow.Shutdown(true, fatalError);            
        }
    }
}
