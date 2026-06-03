namespace EzLib.Models
{
    /// <summary>
    /// Log Viewer 模組設定。
    /// </summary>
    public class LogViewerSettings
    {
        /// <summary>Log Viewer UI 與 API 掛載路徑（預設 /logs）。</summary>
        public string ViewerPath { get; set; } = "/logs";

        /// <summary>宿主專案的 log 檔案目錄，可使用絕對路徑或相對於啟動目錄的相對路徑。</summary>
        public string LogDirectory { get; set; } = "logs";

        /// <summary>要列出的 log 檔搜尋樣式。</summary>
        public string FileSearchPattern { get; set; } = "*.*";

        /// <summary>未指定筆數時預設讀取的最後行數。</summary>
        public int DefaultTailLines { get; set; } = 500;

        /// <summary>單次查詢允許讀取的最大行數。</summary>
        public int MaxTailLines { get; set; } = 5000;

        /// <summary>是否啟用 Log Viewer Key 驗證。</summary>
        public bool RequireViewerKey { get; set; }

        /// <summary>Log Viewer Key，RequireViewerKey 為 true 時請求須帶 X-Log-Viewer-Key header。</summary>
        public string? ViewerKey { get; set; }
    }

    /// <summary>
    /// 可檢視的 log 檔案資訊。
    /// </summary>
    public class LogViewerFile
    {
        /// <summary>檔案名稱，不包含路徑。</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>檔案大小。</summary>
        public long SizeBytes { get; set; }

        /// <summary>最後修改時間（UTC）。</summary>
        public DateTime LastWriteTimeUtc { get; set; }
    }

    /// <summary>
    /// Log 內容讀取條件。
    /// </summary>
    public class LogReadRequest
    {
        /// <summary>要讀取的 log 檔案名稱，不可包含路徑。</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>起始時間（含）。</summary>
        public DateTimeOffset? From { get; set; }

        /// <summary>結束時間（含）。</summary>
        public DateTimeOffset? To { get; set; }

        /// <summary>讀取最後幾行；未指定時使用 DefaultTailLines。</summary>
        public int? TailLines { get; set; }
    }

    /// <summary>
    /// Log 內容讀取結果。
    /// </summary>
    public class LogReadResult
    {
        /// <summary>讀取的檔案名稱。</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>套用篩選後的文字內容。</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>回傳內容的行數。</summary>
        public int LineCount { get; set; }
    }
}
