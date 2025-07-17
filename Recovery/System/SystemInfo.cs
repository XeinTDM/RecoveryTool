using Newtonsoft.Json.Linq;
using System.Management;

namespace RecoveryTool.Recovery.System
{
    public static class SystemInfo
    {
        private static readonly HttpClient client = new();

        public static string GetOperatingSystemInfo()
        {
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (var os in searcher.Get().Cast<ManagementObject>())
            {
                return $"Name: {os["Caption"]}, Version: {os["Version"]}, Architecture: {os["OSArchitecture"]}";
            }
            return "OS Info Not Found";
        }

        public static async Task<string> GetSystemUptimeAsync()
        {
            var searcher = new ManagementObjectSearcher("Select LastBootUpTime From Win32_OperatingSystem");
            foreach (var os in searcher.Get().Cast<ManagementObject>())
            {
                DateTime lastBootUpTime = ManagementDateTimeConverter.ToDateTime(os["LastBootUpTime"].ToString());
                TimeSpan uptime = DateTime.Now - lastBootUpTime;
                return $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes";
            }
            return "Uptime not available";
        }

        public static async Task<string> GetLocationAsync()
        {
            try
            {
                string url = "http://ipinfo.io/json";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string locationData = await response.Content.ReadAsStringAsync();
                JObject locationObject = JObject.Parse(locationData);

                return $"IP Address: {locationObject["ip"]}, " +
                       $"City: {locationObject["city"]}, " +
                       $"Region: {locationObject["region"]}, " +
                       $"Country: {locationObject["country"]}, " +
                       $"Location: {locationObject["loc"]}, " +
                       $"Organization: {locationObject["org"]}, " +
                       $"Postal Code: {locationObject["postal"]}, " +
                       $"Timezone: {locationObject["timezone"]}";
            }
            catch (Exception ex)
            {
                return $"Unable to determine location: {ex.Message}";
            }
        }
    }
}
