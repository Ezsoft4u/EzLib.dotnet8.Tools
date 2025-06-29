using EzLib.Models;
using EzLib.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EzLib.Extensions
{
    /// <summary>
    /// 服務集合擴展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 新增簡訊服務
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configureOptions">設定選項的委派</param>
        /// <returns>服務集合</returns>
        public static IServiceCollection AddSmsService(
            this IServiceCollection services,
            Action<SmsSettings> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            // 註冊設定
            services.Configure<SmsSettings>(configureOptions);
            
            // 註冊 HttpClient 和服務
            services.AddHttpClient<ISmsService, SmsService>();
            
            return services;
        }
        
        /// <summary>
        /// 從配置新增簡訊服務
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configuration">配置</param>
        /// <param name="sectionName">配置區段名稱</param>
        /// <returns>服務集合</returns>
        public static IServiceCollection AddSmsService(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "SmsSettings")
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            // 從配置中綁定設定
            services.Configure<SmsSettings>(configuration.GetSection(sectionName));
            
            // 註冊 HttpClient 和服務
            services.AddHttpClient<ISmsService, SmsService>();
            
            return services;
        }
    }
}