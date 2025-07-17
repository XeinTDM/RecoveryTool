using System.Management;

namespace RecoveryTool.Recovery.System
{
    public static class SystemHelper
    {
        public static async Task<string> GetWmiProperty(string wmiClass, string property)
        {
            string query = $"SELECT {property} FROM {wmiClass}";
            return await ExecuteWmiQuery(query, property);
        }

        private static async Task<string> ExecuteWmiQuery(string query, string property)
        {
            return await Task.Run(() =>
            {
                using var searcher = new ManagementObjectSearcher(query);
                try
                {
                    foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                    {
                        return obj[property]?.ToString() ?? $"{property} Not Available";
                    }
                    return $"{property} Not Found";
                }
                catch (ManagementException ex)
                {
                    return $"Failed to retrieve {property}: {ex.Message}";
                }
            });
        }
    }
}
