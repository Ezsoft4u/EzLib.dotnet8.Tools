using EzLib.Middleware;
using EzLib.Models;
using EzLib.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EzLib.Extensions
{
    /// <summary>
    /// Log Viewer 的 DI 註冊與 Middleware 掛載擴充方法。
    /// </summary>
    public static class LogViewerExtensions
    {
        /// <summary>
        /// 以程式碼設定並註冊 Log Viewer 服務。
        /// </summary>
        public static IServiceCollection AddLogViewer(
            this IServiceCollection services,
            Action<LogViewerSettings> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            var settings = new LogViewerSettings();
            configureOptions(settings);
            return RegisterLogViewerCore(services, settings);
        }

        /// <summary>
        /// 直接指定宿主專案的系統 log 目錄並註冊 Log Viewer 服務。
        /// </summary>
        public static IServiceCollection AddLogViewer(
            this IServiceCollection services,
            string logDirectory)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(logDirectory)) throw new ArgumentException("Log directory is required.", nameof(logDirectory));

            return RegisterLogViewerCore(services, new LogViewerSettings
            {
                LogDirectory = logDirectory
            });
        }

        /// <summary>
        /// 從 IConfiguration 指定區段或單一 log 目錄 key 註冊 Log Viewer 服務。
        /// </summary>
        public static IServiceCollection AddLogViewer(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionNameOrLogDirectoryKey = "LogViewer")
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var settings = new LogViewerSettings();
            var section = configuration.GetSection(sectionNameOrLogDirectoryKey);
            if (section.GetChildren().Any())
            {
                section.Bind(settings);
            }
            else if (!string.IsNullOrWhiteSpace(configuration[sectionNameOrLogDirectoryKey]))
            {
                settings.LogDirectory = configuration[sectionNameOrLogDirectoryKey]!;
            }

            return RegisterLogViewerCore(services, settings);
        }

        /// <summary>
        /// 在 middleware pipeline 掛載 Log Viewer。
        /// </summary>
        public static IApplicationBuilder UseLogViewer(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            return app.UseMiddleware<LogViewerMiddleware>();
        }

        private static IServiceCollection RegisterLogViewerCore(
            IServiceCollection services,
            LogViewerSettings settings)
        {
            services.AddSingleton(settings);
            services.TryAddSingleton<ILogViewerService>(provider =>
            {
                var hostEnvironment = provider.GetService<IHostEnvironment>();
                return hostEnvironment == null
                    ? new FileLogViewerService(settings)
                    : new FileLogViewerService(settings, hostEnvironment);
            });
            return services;
        }
    }
}
