using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzLib.Services
{
    public interface ILogService
    {
        void Information(string message);

        void Warning(string message);

        void Error(string message);
    }
}
