using EzLib.Models;
using Microsoft.Extensions.Options;

namespace EzLib.Services
{
    /// <summary>
    /// 多 LINE bot 服務工廠。
    /// </summary>
    public class LineBotFactory : ILineBotFactory
    {
        internal const string HttpClientName = "EzLib.Line";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LineBotOptions _options;

        /// <summary>
        /// 初始化多 bot 工廠。
        /// </summary>
        public LineBotFactory(
            IHttpClientFactory httpClientFactory,
            IOptions<LineBotOptions> options)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// 依 bot 名稱建立對應 channel 設定的 LINE 服務。
        /// </summary>
        public ILineService GetBot(string botName)
        {
            if (string.IsNullOrWhiteSpace(botName))
                throw new ArgumentException("LINE bot name cannot be empty.", nameof(botName));

            if (!_options.Bots.TryGetValue(botName, out var settings))
                throw new KeyNotFoundException($"LINE bot '{botName}' is not configured.");

            return new LineService(_httpClientFactory.CreateClient(HttpClientName), settings);
        }
    }
}
