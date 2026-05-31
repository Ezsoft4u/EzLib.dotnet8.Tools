using System.Text;
using EzLib.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace EzLib.Services
{
    /// <summary>
    /// LINE webhook event 處理委派。
    /// </summary>
    public delegate Task LineWebhookEventHandler(
        LineWebhookEvent webhookEvent,
        ILineService lineService,
        HttpContext httpContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// LINE webhook endpoint 共用處理流程。
    /// </summary>
    public static class LineWebhookEndpoint
    {
        /// <summary>
        /// 讀取 LINE webhook、驗證簽章，並逐筆派送 event 給使用者 handler。
        /// </summary>
        public static async Task HandleAsync(
            HttpContext httpContext,
            LineWebhookEventHandler handler)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var lineService = httpContext.RequestServices.GetRequiredService<ILineService>();
            await HandleAsync(httpContext, lineService, handler);
        }

        /// <summary>
        /// 使用指定 bot 名稱讀取 LINE webhook、驗證簽章，並逐筆派送 event。
        /// </summary>
        public static async Task HandleAsync(
            HttpContext httpContext,
            string botName,
            LineWebhookEventHandler handler)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));
            if (string.IsNullOrWhiteSpace(botName)) throw new ArgumentException("LINE bot name cannot be empty.", nameof(botName));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var botFactory = httpContext.RequestServices.GetRequiredService<ILineBotFactory>();
            var lineService = botFactory.GetBot(botName);
            await HandleAsync(httpContext, lineService, handler);
        }

        /// <summary>
        /// 使用指定 LINE 服務處理 webhook 共用流程。
        /// </summary>
        private static async Task HandleAsync(
            HttpContext httpContext,
            ILineService lineService,
            LineWebhookEventHandler handler)
        {
            var signature = httpContext.Request.Headers["x-line-signature"].FirstOrDefault();

            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync(httpContext.RequestAborted);

            if (!lineService.ValidateWebhookSignature(body, signature ?? string.Empty))
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var webhook = lineService.ParseWebhook(body);
            foreach (var webhookEvent in webhook.Events)
            {
                await handler(webhookEvent, lineService, httpContext, httpContext.RequestAborted);
            }

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
        }
    }

    /// <summary>
    /// ASP.NET Core minimal API 的 LINE webhook endpoint extension。
    /// </summary>
    public static class LineWebhookEndpointExtensions
    {
        /// <summary>
        /// 註冊基本 LINE webhook POST endpoint，使用者可在 handler 中自行擴充流程。
        /// </summary>
        public static IEndpointConventionBuilder MapLineWebhook(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            LineWebhookEventHandler handler)
        {
            if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("Webhook route pattern cannot be empty.", nameof(pattern));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return endpoints.MapPost(pattern, (HttpContext context) => LineWebhookEndpoint.HandleAsync(context, handler));
        }

        /// <summary>
        /// 註冊指定 bot 的 LINE webhook POST endpoint。
        /// </summary>
        public static IEndpointConventionBuilder MapLineWebhook(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            string botName,
            LineWebhookEventHandler handler)
        {
            if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("Webhook route pattern cannot be empty.", nameof(pattern));
            if (string.IsNullOrWhiteSpace(botName)) throw new ArgumentException("LINE bot name cannot be empty.", nameof(botName));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return endpoints.MapPost(pattern, (HttpContext context) => LineWebhookEndpoint.HandleAsync(context, botName, handler));
        }
    }
}
