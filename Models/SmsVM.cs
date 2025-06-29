using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzLib.Models
{
    /// <summary>
    /// 簡訊發送結果
    /// </summary>
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

        /// <summary>
        /// 送出請求內容
        /// </summary>
        public string? RequestBody { get; set; }

        /// <summary>
        /// 回應內容
        /// </summary>
        public string? ResponseBody { get; set; }

        /// <summary>
        /// 發送成功的訊息ID
        /// </summary>
        public string? MessageId { get; set; }
    }
}
