using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MiracleSticks.Configuration;
using MiracleSticks.Logging;
using MiracleSticksClient.MiracleSticksAPI;
using VncInterop;

namespace MiracleSticksClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainClientWindow : Window
    {
        private static ILogger log = LoggerFactory.GetLogger(LoggerType.File);

        private MiracleSticksAPIClient apiClient;
        private readonly List<ServerRegistration> servers = new List<ServerRegistration>();
        private BackgroundWorker worker;

        public MainClientWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            groupIdToolTip.Text = String.Format(groupIdToolTip.Text, ConfigManager.Data.GroupId);

            serverList.Items.Add("Retrieving server list...");

            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += workerThread_DoWork;
            worker.RunWorkerCompleted += workerThread_RunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if(worker != null)
                worker.CancelAsync();

            if(apiClient != null)
                apiClient.Close();
        }

        private void workerThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null && e.Result is Exception)
            {
                Application.Current.Shutdown(1);
            }
            else if(servers.Count == 0)
            {
                serverList.Items.Clear();
                serverList.Items.Add("No Servers Found");
            }
            else
            {
                serverList.Items.Clear();
                foreach (var server in servers)
                {
                    if (!serverList.Items.Contains(server.ComputerName))
                        serverList.Items.Add(server.ComputerName);
                }
                serverList.SelectedIndex = 0;
                serverList.Focus();

                if (serverList.Items.Count == 1)
                    ConnectButton_Click(null, null);
            }
        }

        private void workerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            QueryResponse queryResponse;
            try
            {
                apiClient = new MiracleSticksAPIClient("BasicHttpBinding_IMiracleSticksAPI");
                
                if (worker.CancellationPending) return;
                
                queryResponse = apiClient.Query(new QueryRequest { GroupID = ConfigManager.Data.GroupId });
                if (queryResponse == null)
                    throw new WebException("Null response received from query request");
            }
            catch (Exception ex)
            {
                log.Write(TraceLevel.Error, "Failed to connect to MiracleSticks servers", ex);
                MessageBox.Show("Failed to connect to MiracleSticks servers", "Fatal Error", MessageBoxButton.OK);
                e.Result = ex;
                return;
            }

            if (worker.CancellationPending) return;

            if (!queryResponse.Success)
            {
                log.Write(TraceLevel.Error, "Query failed: " + queryResponse.Description);
                MessageBox.Show("Failed to query server list: " + queryResponse.Description, "Fatal Error", MessageBoxButton.OK);
                e.Result = new Exception();
                return;
            }

            servers.Clear();
            servers.AddRange(queryResponse.Servers);
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (serverList.SelectedItem != null)
            {
                string serverName = serverList.SelectedItem as string;
                if (serverName != null)
                {
                    // It is possible for a server to appear in the list more than once
                    // In that case, try each one until you succeed
                    // The server will clean up the stale/failed entries
                    var registrations = servers.Where(s => s.ComputerName == serverName);
                    foreach (var registration in registrations)
                    {
                        if (String.IsNullOrEmpty(registration.IPAddress) || registration.Port == 0)
                        {
                            var connResponse = apiClient.Connect(new ConnectRequest
                                                                     {
                                                                         GroupID = ConfigManager.Data.GroupId,
                                                                         SessionId = registration.SessionId
                                                                     });
                            if (connResponse.Success)
                            {
                                Connect(connResponse.RelayIPAddress, Convert.ToUInt16(connResponse.RelayPort), serverName, true);
                                break;
                            }
                            else
                            {
                                MessageBox.Show("Connection failed: " + connResponse.Description, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            Connect(registration.IPAddress, Convert.ToUInt16(registration.Port), serverName, false);
                            break;
                        }
                    }
                }
            }
        }

        private static void Connect(string ipAddress, ushort port, string serverName, bool proxy)
        {
            PasswordDialog passDialog = new PasswordDialog(serverName, proxy);
            passDialog.ShowDialog();
            if(passDialog.ConnectRequested)
            {
                StartVncViewer(ipAddress, port, passDialog.Password);
            }
        }

        private static void StartVncViewer(string ipAddress, ushort port, string password)
        {
            string logDir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            logDir = System.IO.Path.Combine(logDir, FileLogger.LogDirName);
            VncInterop.VncViewer viewer = new VncViewer(new DirectoryInfo(logDir));
            viewer.RunWait(ipAddress, port, password);
        }
    }
}
