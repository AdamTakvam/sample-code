using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using VncInterop;

namespace MiracleSticksServer
{
    public class VncServerManager
    {
        #region Singleton Implementation

        private static VncServerManager instance;
        private static readonly object instanceLock = new object();
        public static VncServerManager Instance
        {
            get
            {
                lock(instanceLock)
                {
                    if(instance == null)
                        instance = new VncServerManager();
                    return instance;
                }
            }
        }

        #endregion

        private const string TightVncRegKey = @"Software\TightVNC\Server";
        private const string LoopBackRegName = "AllowLoopback";
        private const string TrayIconRegName = "RunControlInterface";
        private const int LoopBackRegValue = 1;
        private const int TrayIconRegValue = 0;

        private Thread vncThread;
        private Window vncWindow;
        protected VncServer vncServer;

        private VncServerManager()
        {
            vncWindow = new VncWindow() { Visibility = Visibility.Hidden };
            vncWindow.Show();

            vncServer = new VncServer(vncWindow);
        }

        public static bool IsConfigured
        {
            get { return Registry.CurrentUser.OpenSubKey(TightVncRegKey) != null; }
        }

        public void SetPassword(string password)
        {
            vncServer.SetServerPassword(password);
        }

        public void StartVncServer()
        {
            if (IsVncServerRunning())
                StopVncServer();

            if (vncWindow == null || vncServer == null)
            {
                // Must run on UI thread
                Dispatcher dispatcher = AppMain.ProgressWindow.Dispatcher;
                if (!dispatcher.CheckAccess())
                {
                    Action action = InitVncServer;
                    dispatcher.Invoke(action, null);
                }
                else
                {
                    InitVncServer();
                }
            }

            // The native vnc code will attach this thread to the window supplied during construction and start a new windows message pump
            vncThread = new Thread(vncServer.RunWait) { Name = "VNC UI Thread", IsBackground = true };
            vncThread.Start();

            Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        private void InitVncServer()
        {
            vncWindow = new VncWindow() { Visibility = Visibility.Hidden };
            //vncWindow.Show();
            vncServer = new VncServer(vncWindow);
        }

        public void StopVncServer()
        {
            if (vncThread != null)
            {
                vncServer.Stop();
                vncWindow.Close();

                // It's important to null these things so the garbage collector 
                //  will free the dangling resources (sockets, threads, etc).
                // The nulls also trigger Start() to recreate the now-defunct objects
                vncWindow = null;
                vncThread = null;
                vncServer = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public bool IsVncServerRunning()
        {
            return vncThread != null && vncThread.IsAlive;
        }
    }
}
