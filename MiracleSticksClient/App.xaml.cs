using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using MiracleSticks.Configuration;
using MiracleSticks.Utilities;

namespace MiracleSticksClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class AppMain : Application
    {
        protected void RunApplication(object sender, StartupEventArgs e)
        {
            CommandLineArguments args = new CommandLineArguments(e.Args);

            if(args.IsParamPresent("showconfig"))
            {
                MessageBox.Show(String.Format("Stick ID: {0}\nGroup ID: {1}", ConfigManager.Data.StickId, ConfigManager.Data.GroupId),
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

            if(!String.IsNullOrEmpty(stickId))
            {
                staging = true;
                ConfigManager.Data.StickId = stickId;
            }

            if(staging)
            {
                ConfigManager.Save();
                Shutdown(0);
                return;
            }

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
            if (ConfigManager.Data.Signature != ConfigManager.ComputeSignature(ConfigManager.Data))
            {
                MessageBox.Show("USB key is corrupt: Config file is invalid", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            MainClientWindow mainClientWindow = new MainClientWindow();
            mainClientWindow.Show();
        }
    }
}
