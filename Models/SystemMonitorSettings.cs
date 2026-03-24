// Models/SystemMonitorSettings.cs
using System;
using System.Collections.Generic;

namespace EzLib.Models
{
    /// <summary>
    /// 系統監控模組設定
    /// </summary>
    public class SystemMonitorSettings
    {
        /// <summary>監控 API 端點路徑（預設 /sys/info）</summary>
        public string EndpointPath { get; set; } = "/sys/info";

        /// <summary>是否啟用 API Key 驗證</summary>
        public bool RequireApiKey { get; set; } = false;

        /// <summary>API Key（RequireApiKey 為 true 時生效，請求須帶 X-Api-Key header）</summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// 環境名稱（留空則自動從 ASPNETCORE_ENVIRONMENT 環境變數讀取）
        /// </summary>
        public string? EnvironmentName { get; set; }

        /// <summary>磁碟監控設定</summary>
        public DiskMonitorSettings Disk { get; set; } = new();

        /// <summary>資料庫健康檢查設定</summary>
        public DatabaseMonitorSettings Database { get; set; } = new();

        /// <summary>HTTP 端點健康檢查設定</summary>
        public HttpMonitorSettings Http { get; set; } = new();
    }

    /// <summary>磁碟監控設定</summary>
    public class DiskMonitorSettings
    {
        /// <summary>
        /// 要監控的磁碟路徑清單（例如 ["C:\\", "D:\\"]）。
        /// 留空則自動監控所有 Fixed 類型磁碟。
        /// </summary>
        public List<string> MonitorDrives { get; set; } = new();

        /// <summary>使用率超過此百分比標記為 Warning（0 表示不告警，預設 85）</summary>
        public double WarnIfUsedPercentOver { get; set; } = 85;

        /// <summary>使用率超過此百分比標記為 Critical（0 表示不告警，預設 95）</summary>
        public double CriticalIfUsedPercentOver { get; set; } = 95;
    }

    /// <summary>資料庫監控設定</summary>
    public class DatabaseMonitorSettings
    {
        /// <summary>資料庫連線設定清單</summary>
        public List<DatabaseConnectionSettings> Connections { get; set; } = new();

        /// <summary>加入 SQL Server 連線監控（流式 API）</summary>
        public DatabaseMonitorSettings AddSqlServer(string name, string connectionString, int cacheSeconds = 5)
        {
            Connections.Add(new DatabaseConnectionSettings
            {
                Name = name,
                ConnectionString = connectionString,
                Provider = "SqlServer",
                CacheSeconds = cacheSeconds
            });
            return this;
        }
    }

    /// <summary>單一資料庫連線設定</summary>
    public class DatabaseConnectionSettings
    {
        /// <summary>連線識別名稱</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>連線字串</summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>資料庫提供者（目前支援 SqlServer）</summary>
        public string Provider { get; set; } = "SqlServer";

        /// <summary>健康檢查結果快取秒數，避免頻繁查詢（預設 5 秒）</summary>
        public int CacheSeconds { get; set; } = 5;
    }

    /// <summary>HTTP 端點監控設定</summary>
    public class HttpMonitorSettings
    {
        /// <summary>HTTP 端點設定清單</summary>
        public List<HttpEndpointSettings> Endpoints { get; set; } = new();

        /// <summary>加入 HTTP 端點監控（流式 API）</summary>
        public HttpMonitorSettings AddEndpoint(string name, string url, int timeoutSeconds = 10, int cacheSeconds = 5)
        {
            Endpoints.Add(new HttpEndpointSettings
            {
                Name = name,
                Url = url,
                TimeoutSeconds = timeoutSeconds,
                CacheSeconds = cacheSeconds
            });
            return this;
        }
    }

    /// <summary>單一 HTTP 端點設定</summary>
    public class HttpEndpointSettings
    {
        /// <summary>端點識別名稱</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>端點 URL</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>請求逾時秒數（預設 10 秒）</summary>
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>健康檢查結果快取秒數（預設 5 秒）</summary>
        public int CacheSeconds { get; set; } = 5;
    }
}
