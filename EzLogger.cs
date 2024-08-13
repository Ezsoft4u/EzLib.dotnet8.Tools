using EzLib.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzLib
{
    public class EzLogger
    {
        private ILogService _logService;

        public EzLogger()
        {
            // 使用預設日誌設定
            _logService = new LogService();
        }

        // 初始化日誌服務, 可設定日誌設定
        public void Init(LoggerConfiguration configureLogger)
        {
            _logService = new LogService(configureLogger);
        }

        public void Information(string message)
        {
            _logService.Information(message);
        }

        public void Warning(string message)
        {
            _logService.Warning(message);
        }

        public void Error(string message)
        {
            _logService.Error(message);
        }
    }
}
