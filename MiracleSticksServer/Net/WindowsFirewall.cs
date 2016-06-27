using System;
using System.IO;
using NetFwTypeLib;

namespace MiracleSticksServer.Net
{
    public class WindowsFirewall
    {
        public static void AuthorizeProgram(string title, FileInfo exeFile)
        {
            if (Environment.OSVersion.Version.Major >= 6)
                AuthorizeProgramVista(title, exeFile);
            else
                AuthorizeProgramXP(title, exeFile);
        }

        private static void AuthorizeProgramXP(string title, FileInfo exeFile)
        {
            var mgr = GetFirewallManager();
            var authApps = mgr.LocalPolicy.CurrentProfile.AuthorizedApplications;

            try
            {
                authApps.Item(exeFile.FullName);
            }
            catch(FileNotFoundException)
            {
                var authapp = CreateApplication();
                authapp.Name = title;
                authapp.ProcessImageFileName = exeFile.FullName;
                authapp.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
                authapp.IpVersion = NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY;
                authapp.Enabled = true;
                authApps.Add(authapp);  
            }
        }

        private static void AuthorizeProgramVista(string title, FileInfo exeFile)
        {
            var firewallRule = CreateFirewallRule();
            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            firewallRule.Description = title;
            firewallRule.ApplicationName = exeFile.FullName;
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = "All";
            firewallRule.Name = title;

            var firewallPolicy = GetFirewallPolicy();
            firewallPolicy.Rules.Add(firewallRule);
        }

        private static INetFwRule CreateFirewallRule()
        {
            Type type = Type.GetTypeFromProgID("HNetCfg.FWRule", false);
            return Activator.CreateInstance(type) as INetFwRule;
        }

        private static INetFwPolicy2 GetFirewallPolicy()
        {
            Type type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2", false);
            return Activator.CreateInstance(type) as INetFwPolicy2;
        }

        private static INetFwMgr GetFirewallManager()
        {
            Type type = Type.GetTypeFromProgID("HNetCfg.FwMgr", false);
            return Activator.CreateInstance(type) as INetFwMgr;
        }

        private static INetFwAuthorizedApplication CreateApplication()
        {
            Type type = Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication");
            return Activator.CreateInstance(type) as INetFwAuthorizedApplication;
        }
    }
}
