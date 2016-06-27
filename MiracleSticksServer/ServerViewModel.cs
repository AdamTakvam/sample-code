using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

using MiracleSticks.Configuration;

namespace MiracleSticksServer
{
    public class ServerViewModel : INotifyPropertyChanged
    {
        private const string PasswordPlaceholder = "********";

        public ServerViewModel(ConfigData configData)
        {
            ConfigData = configData;

            _progressText = "Initializing";
            _address = "Not Listening";
            _hasError = false;
            _passwordProtectScreen = VncServerConfig.PasswordProtectScreen;
            _hideDesktopWallpaper = VncServerConfig.HideDesktopWallpaper;
            _grabTransparentWindows = VncServerConfig.GrabTransparentWindows;
            _screenPollingInterval = VncServerConfig.ScreenPollingInterval;
            _discoAction = VncServerConfig.ClientDisconnectAction;
        }

        public ConfigData ConfigData { get; private set; }

        private bool _hasError;
        public bool HasError
        {
            get { return _hasError; }
            set
            {
                _hasError = value;
                OnPropertyChanged("HasError");
            }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                if ((value == false && AppMain.State == AppState.Registered) ||
                    (value == true && AppMain.State == AppState.Initializing))
                {
                    _isRunning = value;
                    OnPropertyChanged("IsRunning");
                }
            }
        }

        public string ComputerName
        {
            get { return ConfigData.ComputerName; }
            set
            {
                if(value != ConfigData.ComputerName)
                {
                    ConfigData.ComputerName = value;
                    ConfigManager.Save();
                }
            }
        }

        public string Password
        {
            get { return PasswordPlaceholder; }
            set
            {
                string newPassHash = Crypto.EncryptPassword(value);
                if (value != PasswordPlaceholder && newPassHash != ConfigData.ServerPassword)
                {
                    VncServerManager.Instance.SetPassword(value);
                    ConfigData.ServerPassword = newPassHash;
                    ConfigManager.Save();
                }
            }
        }

        private string _address;
        public string Address
        {
            get { return _address; }
            set
            {
                _address = value;
                OnPropertyChanged("Address");
            }
        }

        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set 
            { 
                _progress = value;
                OnPropertyChanged("Progress");
            }
        }

        private string _progressText;
        public string ProgressText
        {
            get { return _progressText; }
            set
            {
                _progressText = value;
                OnPropertyChanged("ProgressText");
            }
        }

        private bool _passwordProtectScreen;
        public bool PasswordProtectScreen
        {
            get { return _passwordProtectScreen; }
            set
            {
                if (_passwordProtectScreen != value)
                {
                    _passwordProtectScreen = value;
                    VncServerConfig.PasswordProtectScreen = value;
                }
            }
        }

        private bool _hideDesktopWallpaper;
        public bool HideDesktopWallpaper
        {
            get { return _hideDesktopWallpaper; }
            set
            {
                if (_hideDesktopWallpaper != value)
                {
                    _hideDesktopWallpaper = value;
                    VncServerConfig.HideDesktopWallpaper = value;
                }
            }
        }

        private bool _grabTransparentWindows;
        public bool GrabTransparentWindows
        {
            get { return _grabTransparentWindows; }
            set
            {
                if (_grabTransparentWindows != value)
                {
                    _grabTransparentWindows = value;
                    VncServerConfig.GrabTransparentWindows = value;
                }
            }
        }

        private int _screenPollingInterval;
        public int ScreenPollingInterval
        {
            get { return _screenPollingInterval; }
            set
            {
                if (_screenPollingInterval != value)
                {
                    _screenPollingInterval = value;
                    VncServerConfig.ScreenPollingInterval = value;
                }
            }
        }

        private VncServerConfig.DisconnectAction _discoAction;

        public bool DisconnectAction_Nothing
        {
            get { return _discoAction == VncServerConfig.DisconnectAction.Nothing; }
            set
            {
                if (value && _discoAction != VncServerConfig.DisconnectAction.Nothing)
                {
                    _discoAction = VncServerConfig.DisconnectAction.Nothing;
                    VncServerConfig.ClientDisconnectAction = _discoAction;
                }
            }
        }

        public bool DisconnectAction_LockDesktop
        {
            get { return _discoAction == VncServerConfig.DisconnectAction.LockDesktop; }
            set
            {
                if (value && _discoAction != VncServerConfig.DisconnectAction.LockDesktop)
                {
                    _discoAction = VncServerConfig.DisconnectAction.LockDesktop;
                    VncServerConfig.ClientDisconnectAction = _discoAction;
                }
            }
        }

        public bool DisconnectAction_LogOff
        {
            get { return _discoAction == VncServerConfig.DisconnectAction.LogOff; }
            set
            {
                if (value && _discoAction != VncServerConfig.DisconnectAction.LogOff)
                {
                    _discoAction = VncServerConfig.DisconnectAction.LogOff;
                    VncServerConfig.ClientDisconnectAction = _discoAction;
                }
            }
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void OnPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
