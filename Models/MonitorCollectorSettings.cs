namespace EzLib.Models
{
    /// <summary>儲存後端類型</summary>
    public enum MonitorStorageProvider
    {
        /// <summary>SQL Server（需提供 StorageConnectionString）</summary>
        SqlServer,
        /// <summary>SQLite 本地檔案資料庫（零設定，適合開發/單機部署）</summary>
        Sqlite
    }

    public class MonitorCollectorSettings
    {
        /// <summary>Dashboard UI 掛載路徑（預設 /monitor）</summary>
        public string DashboardPath { get; set; } = "/monitor";

        /// <summary>Dashboard 是否需要 API Key（Header: X-Monitor-Key）</summary>
        public bool RequireDashboardKey { get; set; } = false;

        /// <summary>Dashboard API Key</summary>
        public string? DashboardKey { get; set; }

        /// <summary>資料收集間隔秒數（預設 60 秒）</summary>
        public int CollectIntervalSeconds { get; set; } = 60;

        /// <summary>資料保留天數（預設 30 天，0 表示永久保留）</summary>
        public int RetentionDays { get; set; } = 30;

        /// <summary>
        /// 儲存後端類型（預設 SqlServer）。
        /// 選 Sqlite 時 StorageConnectionString 填入檔案路徑，例如 "monitor.db"。
        /// </summary>
        public MonitorStorageProvider Provider { get; set; } = MonitorStorageProvider.SqlServer;

        /// <summary>
        /// 連線字串（SqlServer）或 SQLite 檔案路徑（Sqlite）。
        /// Sqlite 範例："monitor.db" 或絕對路徑 "C:\data\monitor.db"
        /// </summary>
        public string StorageConnectionString { get; set; } = string.Empty;

        /// <summary>要監控的主機清單</summary>
        public List<MonitorHostSettings> Hosts { get; set; } = new();
    }

    public class MonitorHostSettings
    {
        /// <summary>主機識別名稱（顯示在 Dashboard）</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>SystemMonitor API 完整 URL（例如 https://server1/sys/info）</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>API Key（若目標主機啟用了 RequireApiKey）</summary>
        public string? ApiKey { get; set; }

        /// <summary>請求逾時秒數（預設 15 秒）</summary>
        public int TimeoutSeconds { get; set; } = 15;
    }
}
