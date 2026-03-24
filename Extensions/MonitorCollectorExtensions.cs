using EzLib.BackgroundServices;
using EzLib.Middleware;
using EzLib.Models;
using EzLib.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EzLib.Extensions
{
    /// <summary>
    /// MonitorCollector 模組的 DI 註冊與 Middleware 掛載擴充方法
    /// </summary>
    public static class MonitorCollectorExtensions
    {
        /// <summary>
        /// 以委派方式設定並註冊 MonitorCollector 所需服務。
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configureOptions">設定委派</param>
        /// <returns>服務集合（支援方法串鏈）</returns>
        /// <example>
        /// Program.cs 使用方式：
        /// <code>
        /// builder.Services.AddMonitorCollector(opt =>
        /// {
        ///     opt.StorageConnectionString = "Server=...;Database=...;";
        ///     opt.CollectIntervalSeconds   = 60;
        ///     opt.RetentionDays            = 30;
        ///     opt.Hosts.Add(new MonitorHostSettings
        ///     {
        ///         Name = "WebServer1",
        ///         Url  = "https://server1/sys/info"
        ///     });
        /// });
        /// app.UseMonitorDashboard();
        /// </code>
        /// </example>
        public static IServiceCollection AddMonitorCollector(
            this IServiceCollection services,
            Action<MonitorCollectorSettings> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            var settings = new MonitorCollectorSettings();
            configureOptions(settings);

            return RegisterMonitorCollectorCore(services, settings);
        }

        /// <summary>
        /// 從 <see cref="IConfiguration"/> 讀取設定並註冊 MonitorCollector 所需服務。
        /// 對應 appsettings.json 中的指定 section（預設為 "MonitorCollector"）。
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configuration">應用程式組態</param>
        /// <param name="sectionName">appsettings.json 中的 section 名稱</param>
        /// <returns>服務集合（支援方法串鏈）</returns>
        /// <example>
        /// appsettings.json 範例：
        /// <code>
        /// {
        ///   "MonitorCollector": {
        ///     "StorageConnectionString": "Server=localhost;Database=MyApp;Integrated Security=true;",
        ///     "CollectIntervalSeconds": 60,
        ///     "RetentionDays": 30,
        ///     "DashboardPath": "/monitor",
        ///     "RequireDashboardKey": false,
        ///     "Hosts": [
        ///       { "Name": "WebServer1", "Url": "https://server1/sys/info", "TimeoutSeconds": 15 }
        ///     ]
        ///   }
        /// }
        /// </code>
        /// Program.cs 使用方式：
        /// <code>
        /// builder.Services.AddMonitorCollector(builder.Configuration);
        /// app.UseMonitorDashboard();
        /// </code>
        /// </example>
        public static IServiceCollection AddMonitorCollector(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "MonitorCollector")
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var settings = new MonitorCollectorSettings();
            configuration.GetSection(sectionName).Bind(settings);

            return RegisterMonitorCollectorCore(services, settings);
        }

        /// <summary>
        /// 在 middleware pipeline 掛載 MonitorDashboard。
        /// 必須在 <see cref="AddMonitorCollector"/> 之後呼叫。
        /// </summary>
        /// <param name="app">應用程式建構器</param>
        /// <returns>應用程式建構器（支援方法串鏈）</returns>
        public static IApplicationBuilder UseMonitorDashboard(
            this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            return app.UseMiddleware<MonitorDashboardMiddleware>();
        }

        // ── 內部共用 ──────────────────────────────────────────────────────────

        private static IServiceCollection RegisterMonitorCollectorCore(
            IServiceCollection services,
            MonitorCollectorSettings settings)
        {
            // Settings 以 singleton 注入（Middleware / Service / HostedService 皆直接取用）
            services.AddSingleton(settings);

            // 依 Provider 選擇對應的 Storage 實作
            services.AddSingleton<IMonitorStorageService>(_ =>
                settings.Provider == MonitorStorageProvider.Sqlite
                    ? new SqliteMonitorStorageService(settings.StorageConnectionString)
                    : new MonitorStorageService(settings.StorageConnectionString));

            // IHttpClientFactory（若已註冊則不重複）
            services.AddHttpClient();

            // 收集服務
            services.AddSingleton<MonitorCollectorService>();

            // 背景定時服務
            services.AddHostedService<MonitorCollectorHostedService>();

            return services;
        }
    }
}
