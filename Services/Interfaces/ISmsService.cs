// ISmsService.cs
using EzLib.Models;

namespace EzLib.Services
{
    /// <summary>
    /// 簡訊服務介面
    /// </summary>
    public interface ISmsService
    {
        /// <summary>
        /// 發送簡訊
        /// </summary>
        /// <param name="phoneNumber">接收者手機號碼</param>
        /// <param name="message">簡訊內容</param>
        /// <returns>發送結果</returns>
        Task<SmsResult> SendSmsAsync(string phoneNumber, string message);
    }
}