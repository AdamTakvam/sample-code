using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MiracleSticks.Configuration;

namespace MiracleSticksServer
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class SetupWindow : Window
    {
        private const string PasswordPlaceholder = "***********";

        public SetupWindow()
        {
            InitializeComponent();

            DataContext = ConfigManager.Data;
        }

        public void OnSave(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(computerNameTextBox.Text) ||
                String.IsNullOrWhiteSpace(passwordBox.Password))
            {
                errorField.Visibility = Visibility.Visible;
            }
            else
            {
                errorField.Visibility = Visibility.Hidden;

                if (passwordBox.Password != PasswordPlaceholder)
                {
                    ConfigManager.Data.ServerPassword = Crypto.EncryptPassword(passwordBox.Password);
                    VncServerManager.Instance.SetPassword(passwordBox.Password);
                }

                ConfigManager.Save();

                if (AppMain.State == AppState.Registered)
                    AppMain.ProgressWindow.Start();

                Close();
            }
        }

        public void WindowClosed(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(ConfigManager.Data.ComputerName) || !ConfigManager.Data.IsPasswordSet())
                AppMain.Shutdown(true);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ConfigManager.Data.IsPasswordSet())
                passwordBox.Password = PasswordPlaceholder;
        }
    }
}
