namespace RecoveryTool.Recovery.Gaming
{
    public abstract class BaseService
    {
        protected static async Task CopyDirectoryAsync(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir))
                return;

            Directory.CreateDirectory(destDir);

            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
            }

            foreach (string filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                string newFilePath = filePath.Replace(sourceDir, destDir);
                await CopyFileAsync(filePath, newFilePath);
            }
        }

        protected static async Task CopyFileAsync(string sourceFile, string destFile)
        {
            try
            {
                using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                using var destStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await sourceStream.CopyToAsync(destStream);
            } catch { }
        }

        protected static async Task CopyFilesWithPatternsAsync(string sourceDir, string destDir, IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
            {
                var files = Directory.GetFiles(sourceDir, pattern, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var destinationPath = Path.Combine(destDir, Path.GetFileName(file));
                    await CopyFileAsync(file, destinationPath);
                }
            }
        }

        protected static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }
    }
}