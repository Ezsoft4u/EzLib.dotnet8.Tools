using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzLib.Services
{
    public class LogService : ILogService
    {
        private readonly ILogger _logger;

        public LogService(LoggerConfiguration? loggerConfig = null)
        {
            if (loggerConfig == null)
            {
                // 預設日誌設定
                loggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
            }
            Log.Logger = loggerConfig.CreateLogger();

            _logger = Log.ForContext<LogService>();
        }

        public void Information(string message)
        {
            _logger.Information(message);
        }

        public void Warning(string message)
        {
            _logger.Warning(message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }
    }
}
