using System.Management;
using Microsoft.Win32;
using System.Text;

namespace RecoveryTool.Recovery.System
{
    public static class SoftwareInfo
    {
        public static string GetInstalledSoftware()
        {
            StringBuilder sb = new();
            string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    using RegistryKey subkey = key.OpenSubKey(subkey_name);
                    if (subkey.GetValue("DisplayName") != null)
                    {
                        sb.AppendLine($"Name: {subkey.GetValue("DisplayName")}, Version: {subkey.GetValue("DisplayVersion")}, Install Date: {subkey.GetValue("InstallDate")}");
                    }
                }
            }
            return sb.ToString();
        }

        public static async Task<string> GetSystemUpdatesAsync()
        {
            StringBuilder sb = new();
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_QuickFixEngineering");
                foreach (ManagementObject update in searcher.Get().Cast<ManagementObject>())
                {
                    sb.AppendLine($"Hotfix ID: {update["HotFixID"]}, Description: {update["Description"]}, Installed On: {update["InstalledOn"]}");
                }
            }
            catch (ManagementException ex)
            {
                sb.AppendLine($"Failed to retrieve system updates: {ex.Message}");
            }
            return sb.ToString();
        }
    }
}