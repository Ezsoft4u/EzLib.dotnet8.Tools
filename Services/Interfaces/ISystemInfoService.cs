// Services/Interfaces/ISystemInfoService.cs
using EzLib.Models;

namespace EzLib.Services
{
    /// <summary>
    /// 系統監控服務介面。
    /// 採 pull-based（拉取式）設計：不佔用系統資源，
    /// 只有在呼叫 <see cref="Collect"/> 時才即時收集系統資訊並回傳。
    /// </summary>
    public interface ISystemInfoService
    {
        /// <summary>
        /// 即時收集系統監控資料並回傳快照
        /// </summary>
        /// <returns>包含 System、Process、GC、CPU 四個維度資訊的結果物件</returns>
        SystemMonitorResult Collect();
    }
}
