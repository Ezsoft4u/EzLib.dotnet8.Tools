// Extensions/SystemMonitorExtensions.cs
using EzLib.Models;
using EzLib.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EzLib.Extensions
{
    /// <summary>
    /// SystemMonitor 模組的相依性注入與 Minimal API 端點擴充方法
    /// </summary>
    public static class SystemMonitorExtensions
    {
        /// <summary>
        /// 向 DI 容器註冊 SystemMonitor 所需的服務與設定
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configureOptions">選用的設定委派；未傳入時採預設值</param>
        /// <returns>服務集合（支援方法串鏈）</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> 為 null 時擲出</exception>
        public static IServiceCollection AddSystemMonitor(
            this IServiceCollection services,
            Action<SystemMonitorSettings>? configureOptions = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            if (configureOptions != null)
                services.Configure(configureOptions);
            else
                services.Configure<SystemMonitorSettings>(_ => { });

            services.TryAddSingleton<ISystemInfoService, SystemInfoService>();

            return services;
        }

        /// <summary>
        /// 在 <see cref="WebApplication"/> 上掛載系統監控 Minimal API 端點。
        /// 端點路徑優先使用 <paramref name="path"/> 參數，
        /// 未傳入時使用 <see cref="SystemMonitorSettings.EndpointPath"/>（預設 <c>/sys/info</c>）。
        /// </summary>
        /// <param name="app">WebApplication 實例</param>
        /// <param name="path">覆寫端點路徑（可選）</param>
        /// <returns>WebApplication 實例（支援方法串鏈）</returns>
        /// <exception cref="ArgumentNullException"><paramref name="app"/> 為 null 時擲出</exception>
        /// <summary>
        /// 從 <see cref="IConfiguration"/> 讀取 SystemMonitor 設定並註冊至 DI 容器。
        /// 對應 appsettings.json 中的指定 section（預設為 "SystemMonitor"）。
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configuration">應用程式組態</param>
        /// <param name="sectionName">appsettings.json 中的 section 名稱，預設為 "SystemMonitor"</param>
        /// <returns>服務集合（供鏈式呼叫）</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="services"/> 或 <paramref name="configuration"/> 為 null 時擲出。
        /// </exception>
        /// <example>
        /// appsettings.json 範例：
        /// <code>
        /// {
        ///   "SystemMonitor": {
        ///     "EndpointPath": "/sys/info",
        ///     "RequireApiKey": false,
        ///     "ApiKey": null,
        ///     "Disk": {
        ///       "MonitorDrives": ["C:\\"],
        ///       "WarnIfUsedPercentOver": 85,
        ///       "CriticalIfUsedPercentOver": 95
        ///     },
        ///     "Database": {
        ///       "Connections": [
        ///         {
        ///           "Name": "MainDB",
        ///           "ConnectionString": "Server=localhost;Database=MyApp;Integrated Security=true;",
        ///           "Provider": "SqlServer",
        ///           "CacheSeconds": 5
        ///         }
        ///       ]
        ///     }
        ///   }
        /// }
        /// </code>
        /// Program.cs 使用方式：
        /// <code>
        /// builder.Services.AddSystemMonitor(builder.Configuration);
        /// // 或指定自訂 section 名稱：
        /// builder.Services.AddSystemMonitor(builder.Configuration, "MyMonitor");
        /// </code>
        /// </example>
        public static IServiceCollection AddSystemMonitor(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "SystemMonitor")
        {
            if (services      == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            // 從 config 讀取基本設定（EndpointPath、RequireApiKey、ApiKey、Disk）
            services.Configure<SystemMonitorSettings>(configuration.GetSection(sectionName));

            // 處理 Database:Connections（因為有 AddSqlServer() 流式方法，需要手動讀取並合併）
            var connections = configuration
                .GetSection($"{sectionName}:Database:Connections")
                .Get<List<DatabaseConnectionSettings>>() ?? new List<DatabaseConnectionSettings>();

            services.PostConfigure<SystemMonitorSettings>(opt =>
            {
                if (connections.Any())
                    opt.Database.Connections = connections;
            });

            services.TryAddSingleton<ISystemInfoService, SystemInfoService>();

            return services;
        }

        public static WebApplication MapSystemMonitor(
            this WebApplication app,
            string? path = null)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            // 在啟動時解析設定，確定端點路徑
            var settings     = app.Services.GetRequiredService<IOptions<SystemMonitorSettings>>().Value;
            var endpointPath = path ?? settings.EndpointPath;

            app.MapGet(endpointPath, (
                ISystemInfoService           svc,
                IOptions<SystemMonitorSettings> opts,
                HttpContext                  ctx) =>
            {
                var cfg = opts.Value;

                // API Key 驗證（Header: X-Api-Key）
                if (cfg.RequireApiKey)
                {
                    var provided = ctx.Request.Headers["X-Api-Key"].ToString();
                    if (string.IsNullOrWhiteSpace(provided) || provided != cfg.ApiKey)
                        return Results.Unauthorized();
                }

                return Results.Ok(svc.Collect());
            });

            return app;
        }
    }
}
