using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using MiracleSticks.Configuration;
using MiracleSticks.Logging;
using MiracleSticksServer.MiracleSticksAPI;
using MiracleSticksServer.Net;
using MiracleSticksServer.Relay;

namespace MiracleSticksServer
{
    public class ServerManager
    {
        private readonly ILogger log = LoggerFactory.GetLogger(LoggerType.File);

        private IPEndPoint _externalEP;
        private RelayManager _relayManager;
        private MiracleSticksAPIClient _apiClient;
        private readonly BackgroundWorker _worker;
        private readonly VncServerManager _vncManager;

        public delegate void StartupProgressDelegate(int percentage, string description);
        public event StartupProgressDelegate ProgressChanged;

        public delegate void StartupCompleteDelegate(Exception ex);
        public event StartupCompleteDelegate StartupComplete;

        public ServerManager(VncServerManager vncManager)
        {
            Debug.Assert(vncManager != null);
            _vncManager = vncManager;

            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += workerThread_DoWork;
            _worker.ProgressChanged += OnProgressChanged;
            _worker.RunWorkerCompleted += OnStartupComplete;
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressChanged(e.ProgressPercentage, e.UserState as string);
        }

        private void OnStartupComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            StartupComplete(e.Result as Exception);
        }

        public void Start()
        {
            _worker.RunWorkerAsync();
        }

        public void Shutdown()
        {
            _vncManager.StopVncServer();

            if (_worker != null)
                _worker.CancelAsync();

            if (_apiClient != null)
            {
                try
                {
                    _apiClient.Unregister(new UnregisterRequest
                        {
                            ComputerName = ConfigManager.Data.ComputerName,
                            GroupID = ConfigManager.Data.GroupId
                        });
                } catch {}

                _apiClient.Close();
            }

            if (_relayManager != null)
                _relayManager.Stop();

            if (_externalEP != null)
                UPnP.RemoveMapping(_externalEP.Port);
        }

        private void workerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                log.Write(TraceLevel.Info, "Adding Windows Firewall Rule");

                try
                {
                    WindowsFirewall.AuthorizeProgram(AppMain.DisplayName,
                                                     new FileInfo(Assembly.GetExecutingAssembly().Location));
                }
                catch (Exception ex)
                {
                    log.Write(TraceLevel.Error, "Failed to add rule to Windows firewall", ex);
                }

                if (_worker.CancellationPending) return;
                _worker.ReportProgress(10, "Starting Miracle Server");
                log.Write(TraceLevel.Info, "Starting Miracle Server");

                _vncManager.StartVncServer();

                if (_worker.CancellationPending) return;
                _worker.ReportProgress(30, "Detecting NAT/Firewall");
                log.Write(TraceLevel.Info, "Detecting NAT/Firewall");

                _apiClient = new MiracleSticksAPIClient("BasicHttpBinding_IMiracleSticksAPI");

                // Are we behind a UPnP-enabled NAT/firewall?
                if (UPnP.NatDeviceFound)
                {
                    try
                    {
                        if (_worker.CancellationPending) return;
                        _worker.ReportProgress(50, "Configuring NAT/Firewall");
                        log.Write(TraceLevel.Info, "Configuring NAT/Firewall");

                        _externalEP = UPnP.GetExternalEndpoint(ConfigManager.Data.Port);
                    }
                    catch(Exception ex)
                    {
                        log.Write(TraceLevel.Warning, "UPnP Error", ex);
                    }

                    if (_externalEP == null)
                    {
                        if (_worker.CancellationPending) return;
                        log.Write(TraceLevel.Info, "Creating UPnP port mapping");

                        try
                        {
                            _externalEP = UPnP.MapPort(ConfigManager.Data.Port, ConfigManager.Data.Port);
                        }
                        catch (Exception ex)
                        {
                            log.Write(TraceLevel.Warning, "UPnP Error", ex);
                        }

                        if (_externalEP == null)
                        {
                            log.Write(TraceLevel.Warning, "UPnP port mapping failed");
                            e.Result = RegisterForPacketRelay();
                        }
                    }
                    else
                    {
                        log.Write(TraceLevel.Info, "UPnP port mapping already exists");
                    }

                    if (_externalEP != null)
                    {
                        if (_worker.CancellationPending) return;
                        _worker.ReportProgress(70, "Verifying NAT/Firewall Configuration");

                        // See if the server can access our listen port
                        bool pingSuccess;
                        try
                        {
                            var pingResponse = _apiClient.PortTest(new PortTestRequest {Port = _externalEP.Port});
                            pingSuccess = pingResponse.Success;
                        }
                        catch (EndpointNotFoundException ex)
                        {
                            log.Write(TraceLevel.Error, "Failed to connect to MiracleSticks service", ex);
                            e.Result = new Exception("Failed to connect to MiracleSticks service");
                            return;
                        }

                        if (pingSuccess && !AppMain.DebugMode)
                        {
                            e.Result = RegisterForDirectConnection(_externalEP);
                        }
                        else
                        {
                            log.Write(TraceLevel.Warning, "Port test failed");
                            e.Result = RegisterForPacketRelay();
                        }
                    }
                }
                else
                {
                    if (_worker.CancellationPending) return;
                    log.Write(TraceLevel.Info, "No NAT/firewall detected");

                    // See if the server can access our listen port
                    bool pingSuccess;
                    try
                    {
                        var pingResponse = _apiClient.PortTest(new PortTestRequest {Port = ConfigManager.Data.Port});
                        pingSuccess = pingResponse.Success;
                    }
                    catch (EndpointNotFoundException ex)
                    {
                        log.Write(TraceLevel.Error, "Failed to connect to MiracleSticks service", ex);
                        e.Result = new Exception("Failed to connect to MiracleSticks service");
                        return;
                    }

                    if (pingSuccess && !AppMain.DebugMode)
                    {
                        IPEndPoint ep = new IPEndPoint(NetworkAdapters.GetRoutedInterface(), ConfigManager.Data.Port);
                        e.Result = RegisterForDirectConnection(ep);
                    }
                    else
                    {
                        log.Write(TraceLevel.Warning, "Port test failed");
                        e.Result = RegisterForPacketRelay();
                    }
                }
            }
            catch (ThreadAbortException) { throw; }
            catch (ThreadInterruptedException) { throw; }
            catch(Exception ex)
            {
                log.Write(TraceLevel.Error, "Fatal error", ex);
                e.Result = ex;
            }
        }

        private Exception RegisterForDirectConnection(IPEndPoint ep)
        {
            if (_worker.CancellationPending) return null;
            _worker.ReportProgress(80, "Registering Server");
            log.Write(TraceLevel.Info, "Registering with service (proxy = false)");

            var response = _apiClient.Register(new RegistrationRequest
            {
                ComputerName = ConfigManager.Data.ComputerName,
                StickID = ConfigManager.Data.StickId,
                GroupID = ConfigManager.Data.GroupId,
                Port = ep.Port,
                RequireRelay = false
            });

            if (!response.Success)
            {
                log.Write(TraceLevel.Error, "Registration failed: " + response.Description);
                return new Exception("Registration failed: " + response.Description);
            }
            else
            {
                MainServerWindow.ViewModel.Address = ep.ToString();

                _worker.ReportProgress(100, "Listening for Connections...");
                log.Write(TraceLevel.Info, "Listening for Connections...");
            }
            return null;
        }

        private Exception RegisterForPacketRelay()
        {
            if (_worker.CancellationPending) return null;
            var msgResult = MessageBox.Show(String.Format("Failed to configure NAT/Firewall. " +
                    "MiracleSticks can still connect using a hosted relay service, but performance may be degraded. " +
                    "To ensure optimal performance, either enable UPnP on your NAT/Firewall or manually configure it to forward port {0} to this computer.",
                    ConfigManager.Data.Port),
                "Configuration Warning",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (msgResult == MessageBoxResult.Cancel || _worker.CancellationPending) return null;

            _worker.ReportProgress(80, "Registering Server");
            log.Write(TraceLevel.Info, "Registering with service (proxy = true)");

            var response = _apiClient.Register(new RegistrationRequest
            {
                ComputerName = ConfigManager.Data.ComputerName,
                StickID = ConfigManager.Data.StickId,
                GroupID = ConfigManager.Data.GroupId,
                RequireRelay = true
            });
            if (!response.Success)
            {
                log.Write(TraceLevel.Error, "Registration failed: " + response.Description);
                return new Exception("Registration failed: " + response.Description);
            }
            else
            {
                if (String.IsNullOrWhiteSpace(response.RelayIPAddress) || response.RelayPort == 0)
                {
                    log.Write(TraceLevel.Error, "Relay registration error: Invalid response from server. Please contact technical support.");
                    return new Exception("Relay registration error: Incomplete response from server: " + response.Description);
                }

                log.Write(TraceLevel.Info, String.Format("Connecting to relay: {0}:{1}", response.RelayIPAddress, response.RelayPort));

                var relayHost = Dns.GetHostEntry(response.RelayIPAddress);
                if (relayHost == null || relayHost.AddressList == null || relayHost.AddressList.Length == 0 || relayHost.AddressList[0] == null)
                {
                    log.Write(TraceLevel.Error, "Relay registration error: Could not resolve hostname of relay server: " + response.RelayIPAddress);
                    return new Exception("Relay registration error: Could not resolve hostname of relay server: " + response.RelayIPAddress);
                }

                IPEndPoint relayEP = new IPEndPoint(relayHost.AddressList[0], response.RelayPort);
                _relayManager = new RelayManager();
                if (!_relayManager.Start(relayEP, ConfigManager.Data.Port))
                {
                    log.Write(TraceLevel.Error, "Local relay error: " + _relayManager.ErrorDescription);
                    return new Exception(_relayManager.ErrorDescription);
                }
                else
                {
                    MainServerWindow.ViewModel.Address = "Relay Mode";

                    _worker.ReportProgress(100, "Listening for Connections...");
                    log.Write(TraceLevel.Info, "Listening for Connections...");
                }
            }
            return null;
        }
    }
}
