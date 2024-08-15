using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

/// <summary>
/// HiNet 企業簡訊 API for .NetCore 2.0
/// Web         : https://sms.hinet.net
/// Email       : hiair@hinet.net
/// 
/// Changelog   :
///               2018/04/19 - 初版
/// </summary>

namespace EzLib.Services
{
    /// <summary>
    /// Request類別 
    /// </summary>
    enum RequestType : byte{
        LOGIN = 0,              
        SEND = 1,
        QUERY = 2,
        SENDLONG = 11,
        QUERYLONG = 12,
        SENDFOREIGN = 15,
        CANCEL = 16,
        CANCELLONG = 17,
        SENDFOREIGNLONG = 18
    }

    /// <summary>
    /// 訊息編碼
    /// </summary>
    enum MsgCoding : byte{
        BIG5 = 1,
        BINARY = 2,
        UCS2 = 3,
        UTF8 = 4
    }

    /// <summary>
    /// 其他定數常數 
    /// </summary>
    enum MsgConst : byte{
        UNUSED = 0
    }

    /// <summary>
    /// 資料長度定義 
    /// </summary>
    enum LENGTH: int{
        RESPSET = 80,
        REQSET = 100,
        CONTENT = 160,
        REQUEST = 266,
        RESPONSE = 244,
        SMS_LEN = 70,
        ASCII_SMS_LEN = 159,
        LONG_ASCII_MSG_LEN = 153,
        LONG_MSG_LEN = 67,
        MAX_LONG_SPLIT = 10
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    /// <summary>
    /// 傳送資料到hiAirV2的資料結構 
    /// </summary>
    struct MsgRequest{
        public byte type;           //訊息型態
        public byte coding;         //訊息編碼種類
        public byte priority;       //訊息優先權
        public byte countryCode;    //手機國碼
        public byte setLen;         //set[] 訊息內容的長度
        public byte contentLen;     //content[]訊息內容的長度

        // 訊息相關資料設定
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)LENGTH.REQSET)]
        public byte[] set;

        // 簡訊內容
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)LENGTH.CONTENT)]
        public byte[] content;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    /// <summary>
    /// 從hiAirV2接收資料的資料結構 
    /// </summary>
    struct MsgResponse{
        public byte code;           //回傳訊息代碼
        public byte coding;         //訊息編碼種類
        public byte setLen;         //set[] 訊息內容的長度
        public byte contentLen;     //content[]訊息內容的長度

        // 訊息相關資料
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)LENGTH.RESPSET)]
        public byte[] set;

        // MessageID或其他文字描述
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)LENGTH.CONTENT)]
        public byte[] content;
    }

    /// <summary>
    /// HiNet企業簡訊 Hiair .Net Core API
    /// </summary>
    public class Hiair2Net{

        public Socket socket;

        // 傳送型態
        private const String SEND_NOW               = "01";         //即時傳送
        private const String SEND_EXPIRE            = "02";         //截止重送時間
        private const String SEND_RESERVE           = "03";         //預約傳送
        private const String SEND_RESERVE_EXPIRE    = "04";         //預約傳送+截止重送時間

        // 回覆結果的訊息說明
        private StringBuilder retMessage = new StringBuilder() ;

#pragma warning disable CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮宣告為可為 Null。
        public Hiair2Net() { }
#pragma warning restore CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮宣告為可為 Null。

        /// <summary>
        /// 將MsgRequest結構轉為byte array
        /// </summary>
        /// <param name="req">MsgRequest結構</param>
        /// <returns>byte array</returns>
        byte[] encode(MsgRequest req){
            int size = Marshal.SizeOf(req);
            byte[] buf = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(req, ptr, true);
            Marshal.Copy(ptr, buf, 0, size);
            Marshal.FreeHGlobal(ptr);
            return buf;
        }

        /// <summary>
        /// 將byte array轉為MsgResponse結構
        /// </summary>
        /// <param name="buf">byte array</param>
        /// <returns>MsgResponse結構</returns>
        MsgResponse decode(byte[] buf){
            MsgResponse rep = new MsgResponse();
            int size = buf.Length;

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buf, 0, ptr, size);
#pragma warning disable CS8605 // Unboxing 可能 null 值。
            rep = (MsgResponse)Marshal.PtrToStructure(ptr, rep.GetType());
#pragma warning restore CS8605 // Unboxing 可能 null 值。
            Marshal.FreeHGlobal(ptr);
            return rep;
        }

        /// <summary>
        /// 建立與hiAirV2的連線
        /// </summary>
        /// <param name="serverName">Server IP</param>
        /// <param name="port">Server Port</param>
        /// <param name="userID">登入帳號</param>
        /// <param name="passwd">登入密碼</param>
        /// <returns> 0表示連線正常，-3,-4表示異常或斷線。其餘回傳值請參考規格書</returns>
        public int StartCon(String serverName, int port, String userID, String passwd){
            // 回傳值變數
            int ret = 0;

            int timeout = 1000 * 60; //60sec

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveTimeout = timeout;
            socket.SendTimeout = timeout;

            // 將資料清空
            retMessage.Clear();
            
            try{
                socket.Connect(serverName, port);        
            }catch(Exception ex){
                System.Diagnostics.Debug.WriteLine(ex.Message);
                retMessage  = new StringBuilder("Socket Create Error!");
                return -3;
            }
            
            // 設定登入帳號與密碼 
            String msgSet = String.Format("{0}\0{1}\0", userID, passwd);
            String msgContent = String.Empty;
            
            // 設定Request資料
            MsgRequest req = new MsgRequest();
            req.type = (byte)RequestType.LOGIN;
            req.coding = (byte)MsgCoding.BIG5;
            req.priority = (byte)MsgConst.UNUSED;
            req.countryCode = (byte)MsgConst.UNUSED;
            req.setLen = (byte)msgSet.Length;
            req.contentLen = (byte)msgContent.Length;
            req.set = Encoding.ASCII.GetBytes(msgSet.PadRight( (int)LENGTH.REQSET , '\0'));
            req.content = Encoding.ASCII.GetBytes(msgContent.PadRight((int)LENGTH.CONTENT, '\0'));

            // 傳送與接收資料
            ret = sendToserver(req);

            return ret;
        }

        /// <summary>
        /// 立即傳送文字簡訊 
        /// </summary>
        /// <param name="mobileNum">門號</param>
        /// <param name="SMSMessage">簡訊內容</param>
        /// <returns>0表示訊息成功傳送至主機，其餘回傳值請參考規格書</returns>
        public int SendMsg(String mobileNum, String SMSMessage){
            return this.Send(mobileNum, SMSMessage, String.Empty, String.Empty);
        }

        /// <summary>
        /// 預約傳送文字簡訊 
        /// </summary>
        /// <param name="mobileNum">門號</param>
        /// <param name="SMSMessage">簡訊內容</param>
        /// <param name="reserveTime">預約傳送時間，格式yymmddhhmmss</param>
        /// <returns>0表示訊息成功傳送至主機，其餘回傳值請參考規格書</returns>
        public int SendMsg_Reserve(String mobileNum, String SMSMessage, String reserveTime){
            return this.Send(mobileNum, SMSMessage, reserveTime, String.Empty);
        }
        
        /// <summary>
        /// 立即傳送文字簡訊加重送截止時間 
        /// </summary>
        /// <param name="mobileNum">門號</param>
        /// <param name="SMSMessage">簡訊內容</param>
        /// <param name="expireTime">重送截止時間，單位分鐘0001 ~ 1440</param>
        /// <returns>0表示訊息成功傳送至主機，其餘回傳值請參考規格書</returns>
        public int SendMsg_Expire(String mobileNum, String SMSMessage, String expireTime){
            return this.Send(mobileNum, SMSMessage, String.Empty, expireTime);
        }

        /// <summary>
        /// 預約傳送文字簡訊加重送截止時間 
        /// </summary>
        /// <param name="mobileNum">門號</param>
        /// <param name="SMSMessage">簡訊內容</param>
        /// <param name="reserveTime">預約傳送時間，格式yymmddhhmmss</param>
        /// <param name="expireTime">重送截止時間，單位分鐘0001 ~ 1440</param>
        /// <returns>0表示訊息成功傳送至主機，其餘回傳值請參考規格書</returns>
        public int SendMsg_Reserve_Expire(String mobileNum , String SMSMessage, String reserveTime, String expireTime){
            return this.Send(mobileNum, SMSMessage, reserveTime, expireTime);
        }

        /// <summary>
        /// 傳送文字簡訊(封裝程式僅供內部呼叫用)
        /// </summary>
        /// <param name="mobileNum">門號</param>
        /// <param name="SMSMessage">簡訊內容</param>
        /// <param name="reserveTime">預約傳送時間，格式yymmddhhmmss</param>
        /// <param name="expireTime">重送截止時間，單位分鐘0001 ~ 1440</param>
        /// <returns>0表示訊息成功傳送至主機，其餘回傳值請參考規格書</returns>
        private int Send(String mobileNum, String SMSMessage, String reserveTime, String expireTime){

            int ret = 0;
            retMessage.Clear();

            byte[] utf8 = Encoding.UTF8.GetBytes(SMSMessage);
            byte[] ucs2 = Encoding.GetEncoding("UTF-16BE").GetBytes(SMSMessage);

            int contentLength = 0;

            MsgRequest req = new MsgRequest();

            // 若UTF8 Bytes長度與原資料長度相等，表示為純英數字
            if (utf8.Length == SMSMessage.Length){  //純英數
                req.coding = (byte)MsgCoding.BIG5;    
                contentLength = SMSMessage.Length;
                if (SMSMessage.Length > (int)LENGTH.ASCII_SMS_LEN  ){
                    retMessage  = new StringBuilder("message content length exceeded");
                    return -5;
                }
            }else{
                req.coding = (byte)MsgCoding.UCS2;  //非純英數
                contentLength = ucs2.Length;

                if (SMSMessage.Length > (int)LENGTH.SMS_LEN  ){
                    retMessage  = new StringBuilder("message content length exceeded");
                    return -5;        
                }

            }

            // 帶+號為國外文字簡訊，否則為國內簡訊
            byte msgType = mobileNum.StartsWith("+")  ? (byte)RequestType.SENDFOREIGN : (byte)RequestType.SEND;
            
            String msgSet ;
            
            if ( String.IsNullOrEmpty(reserveTime) && String.IsNullOrEmpty(expireTime) == false ){
                 // 立即傳送       
                msgSet = String.Format("{0}\0{1}\0{2}\0", mobileNum, SEND_EXPIRE, expireTime);
            }else if (String.IsNullOrEmpty(reserveTime)==false && String.IsNullOrEmpty(expireTime) ){    
                // 立即傳送加重送截止時間 
                msgSet = String.Format("{0}\0{1}\0{2}\0", mobileNum, SEND_RESERVE, reserveTime);
            }else if (String.IsNullOrEmpty(reserveTime)==false && String.IsNullOrEmpty(expireTime)==false){
                // 預約傳送
                msgSet = String.Format("{0}\0{1}\0{2}\0{3}\0", mobileNum, SEND_RESERVE_EXPIRE, reserveTime, expireTime);
            }else{
                // 預約傳送加重送截止時間
                msgSet = String.Format("{0}\0{1}\0", mobileNum, SEND_NOW);
            }

            // 設定Request資料
            req.type = msgType;
            req.priority = (byte)MsgConst.UNUSED;
            req.countryCode = (byte)MsgConst.UNUSED;
            req.setLen = (byte)msgSet.Length;
            req.contentLen = (byte)contentLength;
            req.set = Encoding.ASCII.GetBytes(msgSet.PadRight( (int)LENGTH.REQSET, '\0' ));
            if (req.coding == (byte)MsgCoding.UCS2){
                //非純英數字以UCS2 byte傳送
                req.content = Encoding.GetEncoding("UTF-16BE").GetBytes(SMSMessage.PadRight( (int)LENGTH.CONTENT ,'\0'));
            }
            else{
                //純英數字以BIG5 byte傳送
                req.content = Encoding.ASCII.GetBytes(SMSMessage.PadRight( (int)LENGTH.CONTENT ,'\0'));
            }

            // 傳送與接收資料
            ret = this.sendToserver(req);

            return ret;
            
        }

        /// <summary>
        /// 立即傳送長簡訊
        /// <para/>長簡訊中文每個分則長度為67個字，純英數字每個為則為153個字。
        /// <para/>若中文簡訊沒大於70個字，或是純英數字沒大於159個字，請使用一般簡訊SendMsg相關Method。
        /// <para/>否則將被拆為多則傳送或依實際使用則數計費
        /// </summary>
        /// <param name="mobileNum">門號</param>
        /// <param name="SMSMessage">簡訊內容</param>
        /// <returns></returns>
        public int SendMsg_Long(String mobileNum, String SMSMessage){
            return this.SendLong(mobileNum, SMSMessage, String.Empty, String.Empty);
        }

        /// <summary>
        /// 傳送長簡訊加重送期限
        /// <para/>長簡訊中文每個分則長度為67個字，純英數字每個為則為153個字。
        /// <para/>若中文簡訊沒大於70個字，或是純英數字沒大於159個字，請使用一般簡訊SendMsg相關Method。
        /// <para/>否則將被拆為多則傳送或依實際使用則數計費
        /// </summary>
        /// <param name="mobileNum">門號</param>
        /// <param name="SMSMessage">簡訊內容</param>
        /// <param name="expireTime">重送截止時間，單位分鐘0001 ~ 1440</param>
        /// <returns></returns>
        public int SendMsg_Long_Expire(String mobileNum, String SMSMessage, String expireTime){
            return this.SendLong(mobileNum, SMSMessage, String.Empty, expireTime);
        }

        /// <summary>
        /// 傳送長簡訊加預約時間
        /// <para/>長簡訊中文每個分則長度為67個字，純英數字每個為則為153個字。
        /// <para/>若中文簡訊沒大於70個字，或是純英數字沒大於159個字，請使用一般簡訊SendMsg相關Method。
        /// <para/>否則將被拆為多則傳送或依實際使用則數計費
        /// </summary>
        /// <param name="mobileNum">門號</param>
        /// <param name="SMSMessage">簡訊內容</param>
        /// <param name="reserveTime">預約傳送時間，格式yymmddhhmmss</param>
        /// <returns></returns>
        public int SendMsg_Long_Reserve(String mobileNum, String SMSMessage, String reserveTime){
            return this.SendLong(mobileNum, SMSMessage, reserveTime, String.Empty);
        }

        /// <summary>
        /// 傳送長簡訊加預約時間與重送期限
        /// <para/>長簡訊中文每個分則長度為67個字，純英數字每個為則為153個字。
        /// <para/>若中文簡訊沒大於70個字，或是純英數字沒大於159個字，請使用一般簡訊SendMsg相關Method。
        /// <para/>否則將被拆為多則傳送或依實際使用則數計費
        /// </summary>
        /// <param name="mobileNum">門號</param>
        /// <param name="SMSMessage">簡訊內容</param>
        /// <param name="reserveTime">預約傳送時間，格式yymmddhhmmss</param>
        /// <param name="expireTime">重送截止時間，單位分鐘0001 ~ 1440</param>
        /// <returns></returns>
        public int SendMsg_Long_Reserve_Expire(String mobileNum, String SMSMessage, String reserveTime, String expireTime){
            return this.SendLong(mobileNum, SMSMessage, reserveTime, expireTime);
        }

        /// <summary>
        /// 傳送長簡訊(封裝程式僅供內部呼叫用)
        /// <para/>長簡訊中文每個分則長度為67個字，純英數字每個為則為153個字。
        /// <para/>若中文簡訊沒大於70個字，或是純英數字沒大於159個字，請使用一般簡訊SendMsg*。
        /// <para/>否則將被拆為多則傳送或依實際使用則數計費
        /// </summary>
        /// <param name="mobileNum">門號</param>
        /// <param name="SMSMessage">簡訊內容</param>
        /// <param name="reserveTime">預約傳送時間，格式yymmddhhmmss</param>
        /// <param name="expireTime">重送截止時間，單位分鐘0001 ~ 1440</param>
        /// <returns></returns>
        private int SendLong(String mobileNum, String SMSMessage, String reserveTime, String expireTime){
            int ret = 0;
            retMessage.Clear();

            byte[] utf8 = Encoding.UTF8.GetBytes(SMSMessage);
            byte[] ucs2 = Encoding.GetEncoding("UTF-16BE").GetBytes(SMSMessage);
            int msg_len = 0;

            MsgCoding coding;
            if (utf8.Length == SMSMessage.Length){
                coding = MsgCoding.BIG5;
                msg_len = (int)LENGTH.LONG_ASCII_MSG_LEN;   // 純英數字每則分則最長153個字
            }else{
                coding = MsgCoding.UCS2;
                msg_len = (int)LENGTH.LONG_MSG_LEN; // 非純英數字每則分則最長67個字
            }

            // 計算需要拆成多少則分則
            double n = Math.Ceiling( (double)SMSMessage.Length / (double)msg_len );
            double numOfMsg = Math.Round(n);

            // 需要發送numOfMsg+1則
            for (int cnt = 0; cnt <= numOfMsg; cnt++){

                if (cnt==0){
                    // 第一則發送metadata

                    MsgRequest req = new MsgRequest();
                    // 判斷是否為國外門號
                    req.type = mobileNum.StartsWith("+") ? (byte)RequestType.SENDFOREIGNLONG : (byte)RequestType.SENDLONG;

                    String msgSet ;
                    if ( String.IsNullOrEmpty(reserveTime) && String.IsNullOrEmpty(expireTime) == false ){
                        // 立即傳送       
                        msgSet = String.Format("{0}\0{1}\0{2}\0", mobileNum, SEND_EXPIRE, expireTime);
                    }else if (String.IsNullOrEmpty(reserveTime)==false && String.IsNullOrEmpty(expireTime) ){    
                        // 立即傳送加重送截止時間 
                        msgSet = String.Format("{0}\0{1}\0{2}\0", mobileNum, SEND_RESERVE, reserveTime);
                    }else if (String.IsNullOrEmpty(reserveTime)==false && String.IsNullOrEmpty(expireTime)==false){
                        // 預約傳送
                        msgSet = String.Format("{0}\0{1}\0{2}\0{3}\0", mobileNum, SEND_RESERVE_EXPIRE, reserveTime, expireTime);
                    }else{
                        // 預約傳送加重送截止時間
                        msgSet = String.Format("{0}\0{1}\0", mobileNum, SEND_NOW);
                    }

                    // 第一則用不到
                    String msgContent = String.Format("\0");

                    req.coding = (byte)coding;
                    req.priority = (byte) MsgConst.UNUSED;
                    req.countryCode = (byte)MsgConst.UNUSED;
                    req.setLen = (byte)msgSet.Length;
                    req.contentLen = (byte)numOfMsg;    // 填通數
                    req.set = Encoding.ASCII.GetBytes(msgSet.PadRight(  (int)LENGTH.REQSET, '\0'));
                    req.content = Encoding.ASCII.GetBytes(msgContent.PadRight(  (int)LENGTH.CONTENT, '\0'));

                    ret = sendToserver(req);

                }else{
                    // 第二則開始送簡訊內容
                    MsgRequest req = new MsgRequest();  
                    req.type = (byte)cnt;   // 填第幾通
                    req.coding = (byte)coding;
                    req.priority = (byte)MsgConst.UNUSED;
                    req.countryCode = (byte)MsgConst.UNUSED;

                    // 用不到
                    String msgSet = String.Format("\0");
                    req.setLen = (byte)msgSet.Length;
                    req.set = Encoding.ASCII.GetBytes( msgSet.PadRight( (int)LENGTH.REQSET, '\0') );

                    // 取分則內容
                    String partial_msg ;
                    try{
                        partial_msg= SMSMessage.Substring( (cnt-1)*msg_len, msg_len );
                    }catch {
                        // ArgumentOutOfRangeException: 超過指定長度就取到字串結尾
                        partial_msg = SMSMessage.Substring( (cnt-1)*msg_len );
                    }

                    if (req.coding == (byte)MsgCoding.UCS2){
                        // 非純英數字以UCS2 byte傳送
                        byte[] buf = Encoding.GetEncoding("UTF-16BE").GetBytes(partial_msg);
                        req.content = Encoding.GetEncoding("UTF-16BE").GetBytes(partial_msg.PadRight( (int)LENGTH.CONTENT, '\0' ) );
                        req.contentLen = (byte)buf.Length;
                    }else{
                        // 純英數字以BIG5 byte傳送
                        byte[] buf = Encoding.ASCII.GetBytes(partial_msg);
                        req.content = Encoding.ASCII.GetBytes(partial_msg.PadRight( (int)LENGTH.CONTENT, '\0' ) );
                        req.contentLen = (byte)buf.Length;
                    }

                    ret = sendToserver(req);

                }

                if (ret != 0){
                    // 若中途有錯誤直接中斷迴圈不送
                    System.Diagnostics.Debug.WriteLine( String.Format("SendLong() Exit => {0} : {1}", ret, retMessage) );
                    return ret;
                }
            }

            return ret;
        }

        /// <summary>
        /// 傳送與接收資料
        /// </summary>
        /// <param name="req">Request結構</param>
        /// <returns>回應代碼</returns>
        private int sendToserver(MsgRequest req){

            int ret;
            retMessage.Clear();

            // 將MsgRequest結構轉byte array後傳送
            byte[] data = encode(req);
            try{
                int sent = socket.Send(data);
                if (sent != (int)LENGTH.REQUEST){
                    retMessage = new StringBuilder("Socket Send Data Error!");
                    System.Diagnostics.Debug.WriteLine("sent length:" + sent);
                    return -4;
                }        
            }catch{
                retMessage = new StringBuilder("Socket Send Data Error!");
                return -4;
            }
            
            
            // 將接收到的byte arry轉為MsgResponse結構
            byte[] buf = new byte[ (int)LENGTH.RESPONSE];
            try{
                int recv = socket.Receive(buf);
                if (recv != (int)LENGTH.RESPONSE){
                    retMessage = new StringBuilder("Socket Receive Data Error!");
                    System.Diagnostics.Debug.WriteLine("recv length:" + recv);
                    return -4;
                }
            }catch{
                retMessage = new StringBuilder("Socket Receive Data Error!");
                return -4;
            }
            
            MsgResponse rep = decode(buf);
            // 回傳代碼
            ret = rep.code;
            // 回傳的文字描述或MessageID
            retMessage = new StringBuilder( Encoding.ASCII.GetString(rep.content).TrimEnd((Char)0) );
            
            System.Diagnostics.Debug.WriteLine( String.Format("sendToServer() result => {0} : {1}", ret, retMessage) );

            return ret;

        }

        /// <summary>
        /// 查詢一般簡訊狀態
        /// </summary>
        /// <param name="messageID">訊息ID</param>
        /// <returns>0表示訊息已成功送達對方，其餘回傳值請參考規格書</returns>
        public int QueryMsg(String messageID){
            retMessage.Clear();

            return this.Query(RequestType.QUERY, messageID);
            
        }

        /// <summary>
        /// 查詢長簡訊狀態
        /// </summary>
        /// <param name="messageID">訊息ID</param>
        /// <returns>0表示訊息已成功送達對方，其餘回傳值請參考規格書</returns>
        public int QueryLongMsg(String messageID){
            retMessage.Clear();

            return this.Query(RequestType.QUERYLONG, messageID);
        }

        /// <summary>
        /// 查詢簡訊狀態
        /// </summary>
        /// <param name="type">查詢類別</param>
        /// <param name="messageID">訊息ID</param>
        /// <returns>0表示訊息已成功送達對方，其餘回傳值請參考規格書</returns>
        private int Query(RequestType type, String messageID){
            int ret = 0;
            retMessage.Clear();


            String msgSet = String.Format("{0}\0", messageID);
            String msgContent = String.Empty;
            
            // 設定Request資料
            MsgRequest req = new MsgRequest();
            req.type = (byte)type;
            req.coding = (byte)MsgCoding.BIG5;
            req.priority = (byte)MsgConst.UNUSED;
            req.countryCode = (byte)MsgConst.UNUSED;
            req.setLen = (byte)msgSet.Length;
            req.contentLen = (byte)msgContent.Length;
            req.set = Encoding.ASCII.GetBytes( msgSet.PadRight( (int)LENGTH.REQSET, '\0' ) );
            req.content = Encoding.ASCII.GetBytes(msgContent.PadRight( (int)LENGTH.CONTENT, '\0'));

            ret = sendToserver(req);
            return ret;
        }

        /// <summary>
        /// 取消預約一般簡訊 
        /// </summary>
        /// <param name="messageID">訊息ID</param>
        /// <returns>0表示訊息已取消成功，其餘回傳值請參考規格書</returns>
        public int CancelMsg(String messageID){
           retMessage.Clear();

           return this.Cancel(RequestType.CANCEL, messageID);
        }

        /// <summary>
        /// 取消預約長簡訊
        /// </summary>
        /// <param name="messageID">訊息ID</param>
        /// <returns>0表示訊息已取消成功，其餘回傳值請參考規格書</returns>
        public int CancelLongMsg(String messageID){
            retMessage.Clear();

            return this.Cancel(RequestType.CANCELLONG, messageID);
        }

        /// <summary>
        /// 取消預約簡訊(封裝程式僅供內部呼叫用)
        /// </summary>
        /// <param name="type">預約類別</param>
        /// <param name="messageID">訊息ID</param>
        /// <returns>0表示訊息已取消成功，其餘回傳值請參考規格書</returns>
        private int Cancel(RequestType type, String messageID){
            int ret = 0;
            retMessage.Clear();

            String msgSet = String.Format("{0}\0", messageID);
            String msgContent = String.Empty;
            
            // 設定Request資料
            MsgRequest req = new MsgRequest();
            req.type = (byte)type;
            req.coding = (byte)MsgCoding.BIG5;
            req.priority = (byte)MsgConst.UNUSED;
            req.countryCode = (byte)MsgConst.UNUSED;
            req.setLen = (byte)msgSet.Length;
            req.contentLen = (byte)msgContent.Length;
            req.set = Encoding.ASCII.GetBytes( msgSet.PadRight( (int)LENGTH.REQSET, '\0' ) );
            req.content = Encoding.ASCII.GetBytes(msgContent.PadRight( (int)LENGTH.CONTENT, '\0'));

            ret = sendToserver(req);
            return ret;
        }
        
        /// <summary>
        /// 取得描述文字或MessageID
        /// </summary>
        /// <returns>文字或MessageID</returns>
        public String Get_Message(){
            return this.retMessage.ToString();
        }

        /// <summary>
        /// 與簡訊主機中斷連線
        /// </summary>
        public void EndCon(){
            retMessage.Clear();
            if (socket.Connected){
                socket.Disconnect(false);
            }
        }
    }
}