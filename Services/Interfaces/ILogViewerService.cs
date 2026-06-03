using EzLib.Models;

namespace EzLib.Services
{
    /// <summary>
    /// 讀取宿主專案 log 檔案的服務。
    /// </summary>
    public interface ILogViewerService
    {
        /// <summary>
        /// 列出設定目錄中的 log 檔。
        /// </summary>
        Task<IReadOnlyList<LogViewerFile>> ListLogFilesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 依檔名與時間區間讀取 log 內容。
        /// </summary>
        Task<LogReadResult> ReadLogFileAsync(LogReadRequest request, CancellationToken cancellationToken = default);
    }
}
