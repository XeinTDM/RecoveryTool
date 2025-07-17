using System.Net.NetworkInformation;
using System.Management;
using System.Text;

namespace RecoveryTool.Recovery.System
{
    public static class NetworkInfo
    {
        public static async Task<string> GetNetworkAdapterDetailsAsync()
        {
            StringBuilder sb = new();
            var searcher = new ManagementObjectSearcher("Select * From Win32_NetworkAdapter WHERE NetEnabled = TRUE");
            foreach (var adapter in searcher.Get().Cast<ManagementObject>())
            {
                sb.AppendLine($"Name: {adapter["Name"]}, Speed: {Convert.ToInt64(adapter["Speed"]) / 1024 / 1024} Mbps");
            }
            return sb.ToString();
        }

        public static async Task<string> SaveNetworkTrafficInfoAsync(string directoryPath)
        {
            string filePath = Path.Combine(directoryPath, "NetworkTraffic.txt");
            StringBuilder sb = new();
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in interfaces)
            {
                sb.AppendLine($"Interface {ni.Name}:");
                sb.AppendLine($"    Bytes Sent: {ni.GetIPv4Statistics().BytesSent}");
                sb.AppendLine($"    Bytes Received: {ni.GetIPv4Statistics().BytesReceived}");
            }

            await File.WriteAllTextAsync(filePath, sb.ToString());
            return filePath;
        }
    }
}