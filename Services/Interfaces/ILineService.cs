using EzLib.Models;

namespace EzLib.Services
{
    /// <summary>
    /// LINE Messaging API 服務介面。
    /// </summary>
    public interface ILineService
    {
        /// <summary>
        /// 發送文字 push message。
        /// </summary>
        Task<LineSendResult> PushTextMessageAsync(string to, string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 發送原始 LINE messages payload。
        /// </summary>
        Task<LineSendResult> PushMessagesAsync(string to, IEnumerable<object> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// 回覆文字 reply message。
        /// </summary>
        Task<LineSendResult> ReplyTextMessageAsync(string replyToken, string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 回覆原始 LINE messages payload。
        /// </summary>
        Task<LineSendResult> ReplyMessagesAsync(string replyToken, IEnumerable<object> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// 驗證 LINE webhook 簽章。
        /// </summary>
        bool ValidateWebhookSignature(string requestBody, string signature);

        /// <summary>
        /// 解析 LINE webhook body。
        /// </summary>
        LineWebhookRequest ParseWebhook(string requestBody);
    }
}
