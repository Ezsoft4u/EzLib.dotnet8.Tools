using EzLib.Models;
using EzLib.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EzLib.Extensions
{
    /// <summary>
    /// LINE Messaging API DI 註冊擴充方法。
    /// </summary>
    public static class LineServiceCollectionExtensions
    {
        /// <summary>
        /// 新增 LINE Messaging API 服務。
        /// </summary>
        public static IServiceCollection AddLineService(
            this IServiceCollection services,
            Action<LineSettings> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);
            services.Configure<LineBotOptions>(options =>
            {
                var settings = new LineSettings();
                configureOptions(settings);
                options.Bots[LineBotOptions.DefaultBotName] = settings;
            });
            services.AddHttpClient<ILineService, LineService>();
            services.AddHttpClient(LineBotFactory.HttpClientName);
            services.TryAddSingleton<ILineBotFactory, LineBotFactory>();

            return services;
        }

        /// <summary>
        /// 從設定區段新增 LINE Messaging API 服務。
        /// </summary>
        public static IServiceCollection AddLineService(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "LineSettings")
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            services.Configure<LineSettings>(configuration.GetSection(sectionName));
            services.Configure<LineBotOptions>(options =>
            {
                var settings = new LineSettings();
                configuration.GetSection(sectionName).Bind(settings);
                options.Bots[LineBotOptions.DefaultBotName] = settings;
            });
            services.AddHttpClient<ILineService, LineService>();
            services.AddHttpClient(LineBotFactory.HttpClientName);
            services.TryAddSingleton<ILineBotFactory, LineBotFactory>();

            return services;
        }

        /// <summary>
        /// 新增多 LINE bot 服務。
        /// </summary>
        public static IServiceCollection AddLineBots(
            this IServiceCollection services,
            Action<LineBotOptions> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);
            services.AddHttpClient(LineBotFactory.HttpClientName);
            services.TryAddSingleton<ILineBotFactory, LineBotFactory>();

            return services;
        }

        /// <summary>
        /// 從設定區段新增多 LINE bot 服務。
        /// </summary>
        public static IServiceCollection AddLineBots(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "LineBots")
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            services.Configure<LineBotOptions>(options =>
            {
                configuration.GetSection(sectionName).Bind(options.Bots);
            });
            services.AddHttpClient(LineBotFactory.HttpClientName);
            services.TryAddSingleton<ILineBotFactory, LineBotFactory>();

            return services;
        }
    }
}
