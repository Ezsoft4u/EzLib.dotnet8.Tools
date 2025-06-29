// Models/SmsSettings.cs
namespace EzLib.Models
{
    /// <summary>
    /// 簡訊服務設定
    /// </summary>
    /// <remarks>
    /// "SmsSettings": {
    ///  "ApiServer": SmsUrl ,
    ///  "ApiPort": 4443,
    ///  "ApiRoute": RouteUrl,
    ///  "SenderNumber": SenderPhoneNumber,
    ///  "Username": Account,
    ///  "Password": Password
    ///  }
    /// </remarks>
    public class SmsSettings
    {
        /// <summary>
        /// API 伺服器位址
        /// </summary>
        public string? ApiServer { get; set; }

        /// <summary>
        /// API 埠號
        /// </summary>
        public int ApiPort { get; set; }

        /// <summary>
        /// API 路由
        /// </summary>
        public string? ApiRoute { get; set; }

        /// <summary>
        /// 寄件者號碼
        /// </summary>
        public string? SenderNumber { get; set; }

        /// <summary>
        /// 使用者名稱
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// 密碼
        /// </summary>
        public string? Password { get; set; }
    }

    public class SmsSenderSettings
    {
        public SmsSettings? SmsSettings { get; set; }
    }
}