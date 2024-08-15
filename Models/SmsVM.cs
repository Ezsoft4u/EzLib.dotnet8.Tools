using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzLib.Models
{
    public class SmsResult
    {
        /// <summary>
        /// 發送結果
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string? Message { get; set; }
    }
}
