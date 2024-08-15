using System;
using EzLib.Models;
using EzLib.Services;

namespace EzLib
{
    public class SmsSender
    {
        private readonly HiNetSmsSender sender;
        public SmsSender(string serverIp, int port, string username, string password)
        {
            this.sender = new HiNetSmsSender(serverIp, port, username, password);
        }

        public SmsResult SendMessage(string phoneNumber, string message)
        {
            var result = new SmsResult();
            try
            {
                // 連線驗證帳密並回傳狀態碼
                int retCode = sender.Connect();

                // 取得文字描述
                string retContent = sender.GetMessage();

                if (retCode == 0)
                {
                    // 發送文字簡訊並回傳狀態碼
                    retCode = sender.SendMessage(phoneNumber, message);
                    // 取得messageID或文字描述
                    retContent = sender.GetMessage();
                    result.IsSuccess = retCode == 0;
                    result.Message = retContent;
                }
                else
                {
                    // 登入失敗
                    result.IsSuccess = false;
                    result.Message = retContent;
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
            }
            finally
            {
                sender.Disconnect();
            }

            return result;
        }
    }
}
