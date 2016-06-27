using System;
using System.Collections.Generic;
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

namespace MiracleSticksClient
{
    /// <summary>
    /// Password dialog
    /// </summary>
    public partial class PasswordDialog : Window
    {
        public string Password { get; private set; }
        public bool ConnectRequested { get; private set; }

        //private string serverName;
        //private bool proxy;

        public PasswordDialog(string serverName, bool proxy)
        {
            InitializeComponent();

            serverNameLabel.Content = serverName;
            proxyValueLabel.Content = proxy ? "Yes" : "No";
            ConnectRequested = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            passwordBox.Focus();
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(passwordBox.Password))
            {
                MessageBox.Show("You must enter a password.", "Error", MessageBoxButton.OK);
            }
            else
            {
                Password = passwordBox.Password;
                ConnectRequested = true;
                Close();
            }
        }
    }
}
