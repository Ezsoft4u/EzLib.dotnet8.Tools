using System.Net;
using System.Text.Json.Serialization;

namespace EzLib.Models
{
    /// <summary>
    /// LINE Messaging API 設定。
    /// </summary>
    public class LineSettings
    {
        /// <summary>
        /// LINE channel access token，請由安全設定來源注入。
        /// </summary>
        public string? ChannelAccessToken { get; set; }

        /// <summary>
        /// LINE channel secret，用於 webhook 簽章驗證。
        /// </summary>
        public string? ChannelSecret { get; set; }

        /// <summary>
        /// LINE API base URL。
        /// </summary>
        public string ApiBaseUrl { get; set; } = "https://api.line.me";
    }

    /// <summary>
    /// 多 LINE bot 設定集合。
    /// </summary>
    public class LineBotOptions
    {
        /// <summary>
        /// 單 bot 相容模式使用的預設 bot 名稱。
        /// </summary>
        public const string DefaultBotName = "default";

        /// <summary>
        /// 依 bot 名稱保存不同 LINE channel 設定。
        /// </summary>
        public Dictionary<string, LineSettings> Bots { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// LINE 發送 API 回傳結果。
    /// </summary>
    public class LineSendResult
    {
        /// <summary>
        /// API 是否回傳成功狀態碼。
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// HTTP 狀態碼。
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// 結果或錯誤訊息。
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 實際送出的 JSON。
        /// </summary>
        public string? RequestBody { get; set; }

        /// <summary>
        /// LINE API 回應內容。
        /// </summary>
        public string? ResponseBody { get; set; }
    }

    /// <summary>
    /// LINE webhook request。
    /// </summary>
    public class LineWebhookRequest
    {
        /// <summary>
        /// Bot user ID。
        /// </summary>
        [JsonPropertyName("destination")]
        public string? Destination { get; set; }

        /// <summary>
        /// LINE webhook events。
        /// </summary>
        [JsonPropertyName("events")]
        public List<LineWebhookEvent> Events { get; set; } = new();
    }

    /// <summary>
    /// LINE webhook event。
    /// </summary>
    public class LineWebhookEvent
    {
        /// <summary>
        /// Event 類型，例如 message。
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Reply token，可用於 reply API。
        /// </summary>
        [JsonPropertyName("replyToken")]
        public string? ReplyToken { get; set; }

        /// <summary>
        /// Event timestamp。
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; set; }

        /// <summary>
        /// Event 來源。
        /// </summary>
        [JsonPropertyName("source")]
        public LineWebhookSource? Source { get; set; }

        /// <summary>
        /// Message event 的訊息內容。
        /// </summary>
        [JsonPropertyName("message")]
        public LineWebhookMessage? Message { get; set; }
    }

    /// <summary>
    /// LINE event source。
    /// </summary>
    public class LineWebhookSource
    {
        /// <summary>
        /// 來源類型，例如 user、group、room。
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// User ID。
        /// </summary>
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        /// <summary>
        /// Group ID。
        /// </summary>
        [JsonPropertyName("groupId")]
        public string? GroupId { get; set; }

        /// <summary>
        /// Room ID。
        /// </summary>
        [JsonPropertyName("roomId")]
        public string? RoomId { get; set; }
    }

    /// <summary>
    /// LINE webhook message。
    /// </summary>
    public class LineWebhookMessage
    {
        /// <summary>
        /// Message ID。
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Message 類型，例如 text。
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Text message 的文字內容。
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
