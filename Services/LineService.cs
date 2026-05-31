using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EzLib.Models;
using Microsoft.Extensions.Options;

namespace EzLib.Services
{
    /// <summary>
    /// LINE Messaging API 服務。
    /// </summary>
    public class LineService : ILineService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _httpClient;
        private readonly LineSettings _settings;

        /// <summary>
        /// 初始化 LINE Messaging API 服務。
        /// </summary>
        public LineService(HttpClient httpClient, IOptions<LineSettings> options)
            : this(httpClient, options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        /// <summary>
        /// 使用指定設定初始化 LINE Messaging API 服務。
        /// </summary>
        public LineService(HttpClient httpClient, LineSettings settings)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// 發送文字 push message 給指定 user、group 或 room。
        /// </summary>
        public Task<LineSendResult> PushTextMessageAsync(
            string to,
            string message,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(to)) throw new ArgumentException("LINE receiver cannot be empty.", nameof(to));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var payload = new
            {
                to,
                messages = new object[]
                {
                    new { type = "text", text = message }
                }
            };

            return SendLineMessageAsync("/v2/bot/message/push", payload, cancellationToken);
        }

        /// <summary>
        /// 發送原始 LINE messages payload，支援 Flex、template、text 等訊息。
        /// </summary>
        public Task<LineSendResult> PushMessagesAsync(
            string to,
            IEnumerable<object> messages,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(to)) throw new ArgumentException("LINE receiver cannot be empty.", nameof(to));
            var messageList = ValidateMessages(messages);

            var payload = new
            {
                to,
                messages = messageList
            };

            return SendLineMessageAsync("/v2/bot/message/push", payload, cancellationToken);
        }

        /// <summary>
        /// 使用 replyToken 回覆文字訊息。
        /// </summary>
        public Task<LineSendResult> ReplyTextMessageAsync(
            string replyToken,
            string message,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(replyToken)) throw new ArgumentException("LINE reply token cannot be empty.", nameof(replyToken));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var payload = new
            {
                replyToken,
                messages = new object[]
                {
                    new { type = "text", text = message }
                }
            };

            return SendLineMessageAsync("/v2/bot/message/reply", payload, cancellationToken);
        }

        /// <summary>
        /// 使用 replyToken 回覆原始 LINE messages payload。
        /// </summary>
        public Task<LineSendResult> ReplyMessagesAsync(
            string replyToken,
            IEnumerable<object> messages,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(replyToken)) throw new ArgumentException("LINE reply token cannot be empty.", nameof(replyToken));
            var messageList = ValidateMessages(messages);

            var payload = new
            {
                replyToken,
                messages = messageList
            };

            return SendLineMessageAsync("/v2/bot/message/reply", payload, cancellationToken);
        }

        /// <summary>
        /// 使用 channel secret 驗證 LINE webhook 的 x-line-signature。
        /// </summary>
        public bool ValidateWebhookSignature(string requestBody, string signature)
        {
            if (string.IsNullOrEmpty(requestBody)) return false;
            if (string.IsNullOrWhiteSpace(signature)) return false;
            if (string.IsNullOrWhiteSpace(_settings.ChannelSecret)) return false;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.ChannelSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
            var expectedSignature = Convert.ToBase64String(hash);

            var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
            var actualBytes = Encoding.UTF8.GetBytes(signature);
            return expectedBytes.Length == actualBytes.Length
                && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }

        /// <summary>
        /// 將 LINE webhook JSON 解析成簡化模型。
        /// </summary>
        public LineWebhookRequest ParseWebhook(string requestBody)
        {
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new LineWebhookRequest();
            }

            return JsonSerializer.Deserialize<LineWebhookRequest>(requestBody, JsonOptions)
                ?? new LineWebhookRequest();
        }

        /// <summary>
        /// 建立授權後的 LINE HTTP request 並回傳統一結果。
        /// </summary>
        private async Task<LineSendResult> SendLineMessageAsync(
            string path,
            object payload,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_settings.ChannelAccessToken))
            {
                throw new InvalidOperationException("LINE ChannelAccessToken is required.");
            }

            var requestBody = JsonSerializer.Serialize(payload, JsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildApiUri(path))
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ChannelAccessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            return new LineSendResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                Message = response.IsSuccessStatusCode ? "LINE message sent successfully." : response.ReasonPhrase,
                RequestBody = requestBody,
                ResponseBody = responseBody
            };
        }

        /// <summary>
        /// 驗證 LINE messages 數量符合 API 限制。
        /// </summary>
        private static List<object> ValidateMessages(IEnumerable<object> messages)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            var messageList = messages.ToList();
            if (messageList.Count is < 1 or > 5)
                throw new ArgumentException("LINE messages must contain 1 to 5 items.", nameof(messages));

            return messageList;
        }

        /// <summary>
        /// 組合 LINE API 絕對網址，讓 HttpClient 不必依賴 BaseAddress。
        /// </summary>
        private Uri BuildApiUri(string path)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_settings.ApiBaseUrl)
                ? "https://api.line.me"
                : _settings.ApiBaseUrl.TrimEnd('/');

            return new Uri($"{baseUrl}{path}");
        }
    }
}
