using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using MiracleSticks.Logging;
using MiracleSticks.Configuration;
using MiracleSticksServer.MiracleSticksAPI;
using MiracleSticksServer.Net;
using MiracleSticksServer.Relay;
using VncInterop;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MiracleSticksServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainServerWindow : Window
    {
        private readonly ILogger log = LoggerFactory.GetLogger(LoggerType.File);

        private NotifyIcon _notifyIcon;
        private bool _balloonShown;
        private ServerManager server;

        private static readonly ServerViewModel viewModel;
        public static ServerViewModel ViewModel { get { return viewModel; } }

        static MainServerWindow()
        {
            viewModel = new ServerViewModel(ConfigManager.Data);
        }

        public MainServerWindow()
        {
            InitializeComponent();

            DataContext = viewModel;
        }

        #region Minimize to tray

        public void MinimizeToTray()
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Handles the Window's StateChanged event.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void HandleStateChanged(object sender, EventArgs e)
        {
            if (_notifyIcon == null)
            {
                // Initialize NotifyIcon instance "on demand"
                _notifyIcon = new NotifyIcon();
                _notifyIcon.Icon = Properties.Resources.iconMiracleSticks_36x36;
                _notifyIcon.MouseClick += HandleNotifyIconOrBalloonClicked;
                _notifyIcon.BalloonTipClicked += HandleNotifyIconOrBalloonClicked;
            }
            // Update copy of Window Title in case it has changed
            _notifyIcon.Text = Title;

            // Show/hide Window and NotifyIcon
            var minimized = (WindowState == WindowState.Minimized);
            ShowInTaskbar = !minimized;
            _notifyIcon.Visible = minimized;
            if (minimized && !_balloonShown)
            {
                // If this is the first time minimizing to the tray, show the user what happened
                _notifyIcon.ShowBalloonTip(1000, null, Title, ToolTipIcon.None);
                _balloonShown = true;
            }
        }

        /// <summary>
        /// Handles a click on the notify icon or its balloon.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void HandleNotifyIconOrBalloonClicked(object sender, EventArgs e)
        {
            // Restore the Window
            WindowState = WindowState.Normal;
        }
        #endregion

        public void Shutdown(bool kill, bool fatalError)
        {
            if (server != null)
            {
                server.Shutdown();
                server = null;
            }

            if (_notifyIcon != null)
            {
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            if(kill)
                Application.Current.Shutdown(fatalError ? 1 : 0);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Shutdown(true, false);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            passwordBox.Password = viewModel.Password;
        }

        public void StartButton_Click(object sender, RoutedEventArgs e)
        {
            tabControl.SelectedItem = statusTab;
            Start();
        }

        public void Start()
        {
            if(AppMain.State == AppState.Registered)
            {
                Shutdown(false, false);

                // I cannot get VNC to restart no matter what I try; so just restart whole app.
                // The best option I know of is the run VNC in a child AppDomain
                //   but the app mysteriously hangs when I try to create one. FML
                Process newProc = new Process();
                newProc.StartInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().Location);
                newProc.Start(); // the new instance will kill this one.
            }
            else if (AppMain.State == AppState.Initializing)
            {
                AppMain.State = AppState.ConfiguringFirewall;

                if (server != null)
                    server.Shutdown();

                server = new ServerManager(VncServerManager.Instance);
                server.ProgressChanged += ShowStartupProgress;
                server.StartupComplete += OnStartupComplete;
                server.Start();
            }
        }

        private void OnStartupComplete(Exception ex)
        {
            if (ex != null)
            {
                ErrorDialog.ShowDialog(ex);
                Shutdown(true, true);
            }
            else if (viewModel.Progress != 100)
            {
                Shutdown(true, false);
            }
            else
            {
                AppMain.State = AppState.Registered;
                MinimizeToTray();
            }
        }

        private void ShowStartupProgress(int progress, string description)
        {
            viewModel.Progress = progress;
            viewModel.ProgressText = description;
        }

        private void DisconnectAction_Checked(object sender, RoutedEventArgs e)
        {
            FrameworkElement feSource = e.Source as FrameworkElement;
            if (feSource != null)
            {
                switch (feSource.Name)
                {
                    case "DisconnectAction_Nothing":
                        viewModel.DisconnectAction_Nothing = true;
                        break;
                    case "DisconnectAction_LockDesktop":
                        viewModel.DisconnectAction_LockDesktop = true;
                        break;
                    case "DisconnectAction_LogOff":
                        viewModel.DisconnectAction_LogOff = true;
                        break;
                }
            }
        }
    }
}
