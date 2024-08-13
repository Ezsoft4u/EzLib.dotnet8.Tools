using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzLib.Models
{
    public class MailSettings
    {
        /// <summary>
        /// 發送人帳號
        /// </summary>
        public required string Mail { get; set; }
        /// <summary>
        /// 顯示名稱
        /// </summary>
        public string? DisplayName { get; set; }
        /// <summary>
        /// 密碼
        /// </summary>
        public string? Password { get; set; }
        /// <summary>
        /// SMTP 主機
        /// </summary>
        public required string Host { get; set; }
        /// <summary>
        /// SMTP Port
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// SSL 類型
        /// </summary>
        /// <remarks>
        /// 0:None 
        /// 1:Auto 
        /// 2:SslOnConnect 
        /// 3:StartTls 
        /// 4:StartTlsWhenAvailable
        /// </remarks>
        public int SSL { get; set; }
        /// <summary>
        /// 是否啟用 Debug
        /// </summary>
        public bool Debug { get; set; }
        public bool IsAuth { get; set; }
    }


    public class MailResult
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

    public class MailRequest
    {
        public string? From { get; set; }
        public required string ToEmail { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public bool? IsHtml { get; set; }
        public List<IFormFile>? Attachments { get; set; }
    }
}
