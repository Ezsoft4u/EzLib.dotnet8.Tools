// Models/SystemMonitorVM.cs
using System;
using System.Collections.Generic;

namespace EzLib.Models
{
    /// <summary>
    /// 系統基本資訊（機器名稱、OS、.NET 版本、CPU 核心數）
    /// </summary>
    public class SystemInfoVM
    {
        /// <summary>機器名稱</summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>作業系統描述（例如 Windows 10.0.22631）</summary>
        public string OsDescription { get; set; } = string.Empty;

        /// <summary>.NET 執行環境版本（例如 8.0.3）</summary>
        public string DotnetVersion { get; set; } = string.Empty;

        /// <summary>處理器邏輯核心數</summary>
        public int ProcessorCount { get; set; }
    }

    /// <summary>
    /// 目前 Process 執行資訊
    /// </summary>
    public class ProcessInfoVM
    {
        /// <summary>Process 已執行秒數（自啟動至今）</summary>
        public double UptimeSeconds { get; set; }

        /// <summary>Working Set 記憶體使用量（MB）</summary>
        public double WorkingSetMB { get; set; }

        /// <summary>Private Memory 記憶體使用量（MB）</summary>
        public double PrivateMemoryMB { get; set; }

        /// <summary>執行緒數量</summary>
        public int ThreadCount { get; set; }
    }

    /// <summary>
    /// GC（垃圾回收）統計資訊
    /// </summary>
    public class GcInfoVM
    {
        /// <summary>GC Heap 總大小（MB）</summary>
        public double TotalHeapMB { get; set; }

        /// <summary>Gen 0 回收次數</summary>
        public int Gen0Collections { get; set; }

        /// <summary>Gen 1 回收次數</summary>
        public int Gen1Collections { get; set; }

        /// <summary>Gen 2 回收次數</summary>
        public int Gen2Collections { get; set; }
    }

    /// <summary>
    /// CPU 使用率資訊
    /// </summary>
    public class CpuInfoVM
    {
        /// <summary>
        /// CPU 使用率百分比（%）。
        /// 以前後兩次 TotalProcessorTime delta 計算，第一次呼叫回傳 0。
        /// </summary>
        public double UsagePercent { get; set; }
    }

    /// <summary>
    /// 系統實體記憶體資訊
    /// </summary>
    public class SystemRamInfoVM
    {
        /// <summary>系統總實體記憶體（MB）</summary>
        public double TotalMB { get; set; }

        /// <summary>可用實體記憶體（MB）</summary>
        public double AvailableMB { get; set; }

        /// <summary>已使用實體記憶體（MB）</summary>
        public double UsedMB { get; set; }

        /// <summary>記憶體使用率（%）</summary>
        public double UsedPercent { get; set; }
    }

    /// <summary>
    /// 應用程式環境資訊
    /// </summary>
    public class AppInfoVM
    {
        /// <summary>應用程式名稱（Entry Assembly 名稱）</summary>
        public string AppName { get; set; } = string.Empty;

        /// <summary>應用程式版本（Entry Assembly 版本）</summary>
        public string AppVersion { get; set; } = string.Empty;

        /// <summary>環境名稱（例如 Development / Production）</summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>Process 啟動時間（UTC）</summary>
        public DateTime StartedAt { get; set; }
    }

    /// <summary>單顆磁碟空間資訊</summary>
    public class DiskInfoVM
    {
        /// <summary>磁碟根路徑（例如 C:\）</summary>
        public string Drive { get; set; } = string.Empty;

        /// <summary>磁碟類型（Fixed / Network / Removable...）</summary>
        public string DriveType { get; set; } = string.Empty;

        /// <summary>總容量（GB）</summary>
        public double TotalGB { get; set; }

        /// <summary>已使用容量（GB）</summary>
        public double UsedGB { get; set; }

        /// <summary>剩餘可用容量（GB）</summary>
        public double FreeGB { get; set; }

        /// <summary>使用率百分比（%）</summary>
        public double UsedPercent { get; set; }

        /// <summary>狀態：OK / Warning / Critical</summary>
        public string Status { get; set; } = "OK";
    }

    /// <summary>資料庫連線健康檢查結果</summary>
    public class DatabaseInfoVM
    {
        /// <summary>連線名稱（對應 DatabaseConnectionSettings.Name）</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>資料庫是否可連線</summary>
        public bool IsAlive { get; set; }

        /// <summary>健康檢查回應時間（毫秒）</summary>
        public long ResponseMs { get; set; }

        /// <summary>錯誤訊息（IsAlive 為 false 時才有值）</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>是否為快取結果（快取期間內不重新查詢 DB）</summary>
        public bool IsCached { get; set; }
    }

    /// <summary>HTTP 外部端點健康檢查結果</summary>
    public class HttpEndpointInfoVM
    {
        /// <summary>端點名稱</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>端點 URL</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>端點是否可連線（HTTP 2xx 視為正常）</summary>
        public bool IsAlive { get; set; }

        /// <summary>HTTP 回應狀態碼（無法連線時為 null）</summary>
        public int? StatusCode { get; set; }

        /// <summary>回應時間（毫秒）</summary>
        public long ResponseMs { get; set; }

        /// <summary>錯誤訊息（IsAlive 為 false 時才有值）</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>是否為快取結果</summary>
        public bool IsCached { get; set; }
    }

    /// <summary>
    /// 系統監控整合結果
    /// </summary>
    public class SystemMonitorResult
    {
        /// <summary>
        /// 整體健康狀態：Healthy / Degraded / Unhealthy
        /// 依據所有子項目自動判斷：任何 DB/HTTP 不通 → Unhealthy；
        /// 磁碟 Critical → Unhealthy；磁碟 Warning 或 CPU 過高 → Degraded；其餘 → Healthy
        /// </summary>
        public string OverallStatus { get; set; } = "Healthy";

        /// <summary>資料收集時間（UTC）</summary>
        public DateTime CollectedAt { get; set; }

        /// <summary>應用程式環境資訊</summary>
        public AppInfoVM App { get; set; } = new();

        /// <summary>系統基本資訊</summary>
        public SystemInfoVM System { get; set; } = new();

        /// <summary>系統實體記憶體</summary>
        public SystemRamInfoVM SystemRam { get; set; } = new();

        /// <summary>Process 執行資訊</summary>
        public ProcessInfoVM Process { get; set; } = new();

        /// <summary>GC 回收統計資訊</summary>
        public GcInfoVM Gc { get; set; } = new();

        /// <summary>CPU 使用率</summary>
        public CpuInfoVM Cpu { get; set; } = new();

        /// <summary>磁碟空間資訊清單</summary>
        public List<DiskInfoVM> Disks { get; set; } = new();

        /// <summary>資料庫健康檢查清單</summary>
        public List<DatabaseInfoVM> Databases { get; set; } = new();

        /// <summary>HTTP 端點健康檢查清單</summary>
        public List<HttpEndpointInfoVM> Http { get; set; } = new();
    }
}
