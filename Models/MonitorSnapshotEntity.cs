namespace EzLib.Models
{
    public class MonitorSnapshotEntity
    {
        public long Id { get; set; }
        public string HostName { get; set; } = string.Empty;
        public DateTime CollectedAt { get; set; }
        public string OverallStatus { get; set; } = string.Empty;

        // CPU
        public double CpuUsagePercent { get; set; }

        // Process
        public double WorkingSetMB { get; set; }
        public double PrivateMemoryMB { get; set; }
        public int ThreadCount { get; set; }
        public double UptimeSeconds { get; set; }

        // System RAM
        public double SystemRamTotalMB { get; set; }
        public double SystemRamUsedMB { get; set; }
        public double SystemRamUsedPercent { get; set; }

        // GC
        public double GcTotalHeapMB { get; set; }

        // JSON 欄位（Disk / DB / HTTP 結果）
        public string? DisksJson { get; set; }
        public string? DatabasesJson { get; set; }
        public string? HttpJson { get; set; }
    }
}
