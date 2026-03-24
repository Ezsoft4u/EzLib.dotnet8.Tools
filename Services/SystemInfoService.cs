// Services/SystemInfoService.cs
using EzLib.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace EzLib.Services
{
    /// <summary>
    /// 系統監控服務實作（pull-based，呼叫時才收集，無背景執行緒）
    /// </summary>
    public class SystemInfoService : ISystemInfoService
    {
        // ── CPU delta 計算用靜態快取 ──
        private static TimeSpan _lastCpuTime = TimeSpan.Zero;
        private static DateTime _lastSampleAt = DateTime.MinValue;
        private static readonly object _cpuLock = new();

        // ── DB 健康檢查快取 ──
        private static readonly Dictionary<string, (DatabaseInfoVM result, DateTime cachedAt)> _dbCache = new();
        private static readonly object _dbCacheLock = new();

        // ── HTTP 端點健康檢查快取 ──
        private static readonly Dictionary<string, (HttpEndpointInfoVM result, DateTime cachedAt)> _httpCache = new();
        private static readonly object _httpCacheLock = new();

        // ── 共用 HttpClient（避免每次 new）──
        private static readonly HttpClient _httpClient = new(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        private readonly SystemMonitorSettings _settings;

        public SystemInfoService(IOptions<SystemMonitorSettings> options)
        {
            _settings = options.Value;
        }

        /// <inheritdoc/>
        public SystemMonitorResult Collect()
        {
            var now = DateTime.UtcNow;
            var proc = Process.GetCurrentProcess();
            proc.Refresh();

            var result = new SystemMonitorResult
            {
                CollectedAt = now,
                App         = CollectAppInfo(_settings),
                System      = CollectSystemInfo(),
                SystemRam   = CollectSystemRam(),
                Process     = CollectProcessInfo(proc),
                Gc          = CollectGcInfo(),
                Cpu         = CollectCpuInfo(proc, now),
                Disks       = CollectDisks(_settings.Disk),
                Databases   = CollectDatabases(_settings.Database),
                Http        = CollectHttpEndpoints(_settings.Http),
            };

            result.OverallStatus = DetermineOverallStatus(result);
            return result;
        }

        // ── 整體健康狀態 ──────────────────────────────────────────────

        private static string DetermineOverallStatus(SystemMonitorResult r)
        {
            if (r.Databases.Any(d => !d.IsAlive))       return "Unhealthy";
            if (r.Http.Any(h => !h.IsAlive))            return "Unhealthy";
            if (r.Disks.Any(d => d.Status == "Critical")) return "Unhealthy";
            if (r.Disks.Any(d => d.Status == "Warning")) return "Degraded";
            if (r.Cpu.UsagePercent > 90)                 return "Degraded";
            return "Healthy";
        }

        // ── 應用程式環境資訊 ──────────────────────────────────────────

        private static AppInfoVM CollectAppInfo(SystemMonitorSettings settings)
        {
            var asm = Assembly.GetEntryAssembly();
            var proc = Process.GetCurrentProcess();
            var envName = settings.EnvironmentName
                ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Unknown";

            return new AppInfoVM
            {
                AppName    = asm?.GetName().Name ?? proc.ProcessName,
                AppVersion = asm?.GetName().Version?.ToString() ?? "Unknown",
                Environment = envName,
                StartedAt  = proc.StartTime.ToUniversalTime(),
            };
        }

        // ── 系統基本資訊 ──────────────────────────────────────────────

        private static SystemInfoVM CollectSystemInfo() => new()
        {
            MachineName    = System.Environment.MachineName,
            OsDescription  = RuntimeInformation.OSDescription,
            DotnetVersion  = System.Environment.Version.ToString(),
            ProcessorCount = System.Environment.ProcessorCount,
        };

        // ── 系統實體記憶體（Windows P/Invoke）────────────────────────

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint   dwLength;
            public uint   dwMemoryLoad;
            public ulong  ullTotalPhys;
            public ulong  ullAvailPhys;
            public ulong  ullTotalPageFile;
            public ulong  ullAvailPageFile;
            public ulong  ullTotalVirtual;
            public ulong  ullAvailVirtual;
            public ulong  ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        private static SystemRamInfoVM CollectSystemRam()
        {
            var vm = new SystemRamInfoVM();
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
                    if (GlobalMemoryStatusEx(ref memStatus))
                    {
                        var totalMB     = memStatus.ullTotalPhys / 1_048_576.0;
                        var availableMB = memStatus.ullAvailPhys  / 1_048_576.0;
                        var usedMB      = totalMB - availableMB;

                        vm.TotalMB     = Math.Round(totalMB, 0);
                        vm.AvailableMB = Math.Round(availableMB, 0);
                        vm.UsedMB      = Math.Round(usedMB, 0);
                        vm.UsedPercent = totalMB > 0
                            ? Math.Round(usedMB / totalMB * 100.0, 1)
                            : 0;
                    }
                }
                else if (File.Exists("/proc/meminfo"))
                {
                    // Linux fallback
                    var lines = File.ReadAllLines("/proc/meminfo");
                    long totalKb = 0, availKb = 0;
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("MemTotal:"))
                            totalKb = ParseKbValue(line);
                        else if (line.StartsWith("MemAvailable:"))
                            availKb = ParseKbValue(line);
                    }
                    if (totalKb > 0)
                    {
                        var totalMB     = totalKb / 1024.0;
                        var availableMB = availKb / 1024.0;
                        var usedMB      = totalMB - availableMB;
                        vm.TotalMB      = Math.Round(totalMB, 0);
                        vm.AvailableMB  = Math.Round(availableMB, 0);
                        vm.UsedMB       = Math.Round(usedMB, 0);
                        vm.UsedPercent  = Math.Round(usedMB / totalMB * 100.0, 1);
                    }
                }
            }
            catch { /* 無法取得時回傳空值 */ }
            return vm;
        }

        private static long ParseKbValue(string line)
        {
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 && long.TryParse(parts[1], out var val) ? val : 0;
        }

        // ── Process 資訊 ──────────────────────────────────────────────

        private static ProcessInfoVM CollectProcessInfo(Process proc)
        {
            var uptime = DateTime.UtcNow - proc.StartTime.ToUniversalTime();
            return new ProcessInfoVM
            {
                UptimeSeconds   = Math.Round(uptime.TotalSeconds, 1),
                WorkingSetMB    = Math.Round(proc.WorkingSet64        / 1_048_576.0, 2),
                PrivateMemoryMB = Math.Round(proc.PrivateMemorySize64 / 1_048_576.0, 2),
                ThreadCount     = proc.Threads.Count,
            };
        }

        // ── GC 統計 ───────────────────────────────────────────────────

        private static GcInfoVM CollectGcInfo() => new()
        {
            TotalHeapMB     = Math.Round(GC.GetTotalMemory(false) / 1_048_576.0, 2),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
        };

        // ── CPU 使用率（delta）────────────────────────────────────────

        private static CpuInfoVM CollectCpuInfo(Process proc, DateTime nowUtc)
        {
            double usagePercent = 0;
            lock (_cpuLock)
            {
                var currentCpuTime = proc.TotalProcessorTime;
                var elapsedSeconds = (nowUtc - _lastSampleAt).TotalSeconds;

                if (_lastSampleAt != DateTime.MinValue && elapsedSeconds > 0.1)
                {
                    var cpuDelta = (currentCpuTime - _lastCpuTime).TotalSeconds;
                    usagePercent = cpuDelta / (elapsedSeconds * System.Environment.ProcessorCount) * 100.0;
                    usagePercent = Math.Round(Math.Clamp(usagePercent, 0.0, 100.0), 1);
                }
                _lastCpuTime  = currentCpuTime;
                _lastSampleAt = nowUtc;
            }
            return new CpuInfoVM { UsagePercent = usagePercent };
        }

        // ── 磁碟空間 ──────────────────────────────────────────────────

        private static List<DiskInfoVM> CollectDisks(DiskMonitorSettings s)
        {
            var result = new List<DiskInfoVM>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (s.MonitorDrives.Count == 0)
                {
                    if (drive.DriveType != DriveType.Fixed) continue;
                }
                else
                {
                    var name    = drive.Name.TrimEnd('\\', '/');
                    var matched = s.MonitorDrives.Any(m =>
                        string.Equals(m.TrimEnd('\\', '/'), name, StringComparison.OrdinalIgnoreCase));
                    if (!matched) continue;
                }

                if (!drive.IsReady) continue;

                var total   = drive.TotalSize;
                var free    = drive.AvailableFreeSpace;
                var used    = total - free;
                var usedPct = total > 0 ? Math.Round((double)used / total * 100.0, 1) : 0.0;

                var status = "OK";
                if (s.CriticalIfUsedPercentOver > 0 && usedPct >= s.CriticalIfUsedPercentOver)
                    status = "Critical";
                else if (s.WarnIfUsedPercentOver > 0 && usedPct >= s.WarnIfUsedPercentOver)
                    status = "Warning";

                result.Add(new DiskInfoVM
                {
                    Drive       = drive.Name,
                    DriveType   = drive.DriveType.ToString(),
                    TotalGB     = Math.Round(total / 1_073_741_824.0, 2),
                    UsedGB      = Math.Round(used  / 1_073_741_824.0, 2),
                    FreeGB      = Math.Round(free  / 1_073_741_824.0, 2),
                    UsedPercent = usedPct,
                    Status      = status,
                });
            }
            return result;
        }

        // ── SQL Server 健康檢查 ───────────────────────────────────────

        private static List<DatabaseInfoVM> CollectDatabases(DatabaseMonitorSettings s)
        {
            var result = new List<DatabaseInfoVM>();
            foreach (var conn in s.Connections)
                result.Add(CheckDatabase(conn));
            return result;
        }

        private static DatabaseInfoVM CheckDatabase(DatabaseConnectionSettings cfg)
        {
            lock (_dbCacheLock)
            {
                if (_dbCache.TryGetValue(cfg.Name, out var cached) &&
                    (DateTime.UtcNow - cached.cachedAt).TotalSeconds < cfg.CacheSeconds)
                {
                    return new DatabaseInfoVM
                    {
                        Name = cached.result.Name, IsAlive = cached.result.IsAlive,
                        ResponseMs = cached.result.ResponseMs, ErrorMessage = cached.result.ErrorMessage,
                        IsCached = true,
                    };
                }
            }

            var vm = new DatabaseInfoVM { Name = cfg.Name };
            var sw = Stopwatch.StartNew();
            try
            {
                using var conn = new SqlConnection(cfg.ConnectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.ExecuteScalar();
                sw.Stop();
                vm.IsAlive  = true;
                vm.ResponseMs = sw.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                sw.Stop();
                vm.IsAlive      = false;
                vm.ResponseMs   = sw.ElapsedMilliseconds;
                vm.ErrorMessage = ex.Message;
            }

            lock (_dbCacheLock) { _dbCache[cfg.Name] = (vm, DateTime.UtcNow); }
            return vm;
        }

        // ── HTTP 端點健康檢查 ─────────────────────────────────────────

        private static List<HttpEndpointInfoVM> CollectHttpEndpoints(HttpMonitorSettings s)
        {
            var result = new List<HttpEndpointInfoVM>();
            foreach (var ep in s.Endpoints)
                result.Add(CheckHttpEndpoint(ep));
            return result;
        }

        private static HttpEndpointInfoVM CheckHttpEndpoint(HttpEndpointSettings cfg)
        {
            lock (_httpCacheLock)
            {
                if (_httpCache.TryGetValue(cfg.Name, out var cached) &&
                    (DateTime.UtcNow - cached.cachedAt).TotalSeconds < cfg.CacheSeconds)
                {
                    var c = cached.result;
                    return new HttpEndpointInfoVM
                    {
                        Name = c.Name, Url = c.Url, IsAlive = c.IsAlive,
                        StatusCode = c.StatusCode, ResponseMs = c.ResponseMs,
                        ErrorMessage = c.ErrorMessage, IsCached = true,
                    };
                }
            }

            var vm = new HttpEndpointInfoVM { Name = cfg.Name, Url = cfg.Url };
            var sw = Stopwatch.StartNew();
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(
                    TimeSpan.FromSeconds(cfg.TimeoutSeconds));
                var response = _httpClient.GetAsync(cfg.Url, cts.Token).GetAwaiter().GetResult();
                sw.Stop();
                vm.StatusCode = (int)response.StatusCode;
                vm.IsAlive    = response.IsSuccessStatusCode;
                vm.ResponseMs = sw.ElapsedMilliseconds;
                if (!vm.IsAlive)
                    vm.ErrorMessage = $"HTTP {vm.StatusCode}";
            }
            catch (Exception ex)
            {
                sw.Stop();
                vm.IsAlive      = false;
                vm.ResponseMs   = sw.ElapsedMilliseconds;
                vm.ErrorMessage = ex.Message;
            }

            lock (_httpCacheLock) { _httpCache[cfg.Name] = (vm, DateTime.UtcNow); }
            return vm;
        }
    }
}
