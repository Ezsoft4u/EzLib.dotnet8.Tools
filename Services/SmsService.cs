// SmsService.cs (範例實作，使用 HttpClient)
using EzLib.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EzLib.Services
{
    public class SmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly SmsSettings _smsSettings;
        private string _reqBodyString = string.Empty;
        private string _resBodyString = string.Empty;

        /// <summary>
        /// 初始化簡訊服務
        /// </summary>
        /// <param name="httpClient">HTTP客戶端</param>
        /// <param name="options">簡訊設定選項</param>
        public SmsService(HttpClient httpClient, IOptions<SmsSettings> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _smsSettings = options?.Value ?? throw new ArgumentNullException(nameof(options));

            // 確保編碼提供者已註冊
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// 發送簡訊
        /// </summary>
        /// <param name="phoneNumber">接收者手機號碼</param>
        /// <param name="message">簡訊內容</param>
        /// <returns>發送結果</returns>
        public async Task<SmsResult> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // 建立完整的 API URL
                string apiUrl = $"{_smsSettings.ApiServer}:{_smsSettings.ApiPort}{_smsSettings.ApiRoute}";

                // 直接將字串轉換為 UTF-16BE 格式的位元組
                byte[] utf16beBytes = Encoding.BigEndianUnicode.GetBytes(message);
                // 將這些位元組轉換為 Hex 字串表示形式
                string messageuft16be = BitConverter.ToString(utf16beBytes).Replace("-", "");

                // 建立 x-www-form-urlencoded 格式的 body
                var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "account", _smsSettings.Username },
                    { "password", _smsSettings.Password },
                    { "from_addr", _smsSettings.SenderNumber },
                    { "to_addr", phoneNumber },
                    { "msg_dcs", "8" },
                    { "msg", messageuft16be }
                });

                // 送出前將requestBody轉換為字串以便於除錯
                _reqBodyString = await requestBody.ReadAsStringAsync();

                _resBodyString = string.Empty; // 清空回應內容
                var response = await _httpClient.PostAsync(apiUrl, requestBody);
                if (response.IsSuccessStatusCode)
                {
                    // 將回應內容轉換為字串以便於除錯
                    var responseBytes = await response.Content.ReadAsByteArrayAsync();
                    var big5 = CodePagesEncodingProvider.Instance.GetEncoding("big5");
                    _resBodyString = big5.GetString(responseBytes);
                    return ParseSmsResponse(_resBodyString);
                }
                else
                {
                    return new SmsResult
                    {
                        IsSuccess = false,
                        Message = $"外部 API 錯誤: {response.StatusCode} - {response.ReasonPhrase}",
                        RequestBody = _reqBodyString,
                        ResponseBody = _resBodyString,
                    };
                }
            }
            catch (Exception ex)
            {
                return new SmsResult
                {
                    IsSuccess = false,
                    Message = $"例外狀況: {ex.Message}",
                    RequestBody = _reqBodyString,
                    ResponseBody = _resBodyString,
                };
            }
        }

        private SmsResult ParseSmsResponse(string responseContent)
        {
            try
            {
                // 將 <br> 替換為 <br/> 以符合 XML 格式
                string fixedContent = responseContent.Replace("<br>", "<br/>");
                XDocument doc = XDocument.Parse(fixedContent);
                var body = doc.Descendants("body").FirstOrDefault();

                if (body != null)
                {
                    string bodyContent = body.Value.Trim();
                    // 判斷是成功還是失敗的回應格式
                    // 失敗只會有一個'|'
                    // 成功會有多個'|'
                    // 成功的回應格式: {to_addr}|{return code}|{messageid}|{description}
                    // 失敗的回應格式: {return code}|{description}
                    // 解析回應內容
                    if (string.IsNullOrEmpty(bodyContent))
                    {
                        return new SmsResult
                        {
                            IsSuccess = false,
                            Message = "回應內容 body 為空",
                            RequestBody = _reqBodyString,
                            ResponseBody = _resBodyString,
                        };
                    }

                    if (bodyContent.Split('|').Length > 2)
                    {
                        // 成功的回應格式
                        /*
                            <html>
                            <header>
                            </header>
                            <body>
                            0926666905|0|H45FB24C|Success<br>
                            </body>
                            </html>
                        */
                        return ParseSuccessResponse(bodyContent);
                    }
                    else
                    {
                        // 失敗的回應格式
                        // {return code}|{description}
                        return ParseFailureResponse(bodyContent);
                    }
                }
                else
                {
                    return new SmsResult
                    {
                        IsSuccess = false,
                        Message = "回應內容 body 為空",
                        RequestBody = _reqBodyString,
                        ResponseBody = _resBodyString,
                    };
                }
            }
            catch (Exception ex)
            {
                return new SmsResult
                {
                    IsSuccess = false,
                    Message = $"解析回應內容時發生錯誤: {ex.Message}",
                    RequestBody = _reqBodyString,
                    ResponseBody = _resBodyString,

                };
            }
        }

        private SmsResult ParseSuccessResponse(string bodyContent)
        {
            var parts = bodyContent.Split(new[] { "<br/>" }, StringSplitOptions.RemoveEmptyEntries);
            bool allSuccess = true;
            StringBuilder messages = new StringBuilder();
            var messageIds = new List<string>();

            foreach (var part in parts)
            {
                var values = part.Split('|');
                if (values.Length == 4)
                {
                    string toAddr = values[0];
                    string returnCode = values[1];
                    string messageId = values[2];
                    string description = values[3];

                    if (returnCode != "0") // 假設 returnCode 0 代表成功
                    {
                        allSuccess = false;
                        messages.AppendLine($"發送至 {toAddr} 失敗: {description} (Return Code: {returnCode})");
                    }
                    else
                    {
                        messageIds.Add(messageId);
                        messages.AppendLine($"發送至 {toAddr} 成功，訊息 ID: {messageId}");
                    }
                }
                else
                {
                    allSuccess = false;
                    messages.AppendLine($"無法解析的回應: {part}");
                }
            }

            return new SmsResult
            {
                IsSuccess = allSuccess,
                Message = messages.ToString(),
                RequestBody = _reqBodyString,
                ResponseBody = _resBodyString,
                MessageId = string.Join(", ", messageIds)
            };
        }

        private SmsResult ParseFailureResponse(string bodyContent)
        {
            var values = bodyContent.Split('|');
            if (values.Length == 2)
            {
                string returnCode = values[0];
                string description = values[1];

                return new SmsResult
                {
                    IsSuccess = false,
                    Message = $"發送失敗: {description} (Return Code: {returnCode})",
                    RequestBody = _reqBodyString,
                    ResponseBody = _resBodyString,
                };
            }
            else
            {
                return new SmsResult
                {
                    IsSuccess = false,
                    Message = $"無法解析的回應: {bodyContent}",
                    RequestBody = _reqBodyString,
                    ResponseBody = _resBodyString,
                };
            }
        }
    }
}