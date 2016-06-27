using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace MiracleSticksServer
{
    internal static class VncServerConfig
    {
        private const string RegKeyName = @"Software\TightVNC\Server\";

        private enum ConfigKey
        {
            BlankScreen,
            RemoveWallpaper,
            GrabTransparentWindows,
            PollingInterval,
            DisconnectAction
        }

        public enum DisconnectAction : int
        {
            Nothing = 0,
            LockDesktop = 1,
            LogOff = 2
        }

        public static bool PasswordProtectScreen
        {
            get { return GetBoolValue(ConfigKey.BlankScreen); }
            set { SetBoolValue(ConfigKey.BlankScreen, value); }
        }

        public static bool HideDesktopWallpaper
        {
            get { return GetBoolValue(ConfigKey.RemoveWallpaper); }
            set { SetBoolValue(ConfigKey.RemoveWallpaper, value); }
        }

        public static bool GrabTransparentWindows
        {
            get { return GetBoolValue(ConfigKey.GrabTransparentWindows); }
            set { SetBoolValue(ConfigKey.GrabTransparentWindows, value); }
        }

        public static int ScreenPollingInterval
        {
            get { return GetIntValue(ConfigKey.PollingInterval); }
            set { SetIntValue(ConfigKey.PollingInterval, value); }
        }

        public static DisconnectAction ClientDisconnectAction
        {
            get { return (DisconnectAction)GetIntValue(ConfigKey.DisconnectAction); }
            set { SetIntValue(ConfigKey.DisconnectAction, (int)value); }
        }

        private static bool GetBoolValue(ConfigKey key)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(RegKeyName);
            return regKey != null && ((int)regKey.GetValue(key.ToString()) != 0);
        }

        private static int GetIntValue(ConfigKey key)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(RegKeyName);
            return regKey != null ? (int)regKey.GetValue(key.ToString()) : 0;
        }

        private static void SetBoolValue(ConfigKey key, bool value)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(RegKeyName, true);
            if(regKey != null)
                regKey.SetValue(key.ToString(), value ? 1 : 0);
        }

        private static void SetIntValue(ConfigKey key, int value)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(RegKeyName, true);
            if (regKey != null)
                regKey.SetValue(key.ToString(), value);
        }

    }
}
