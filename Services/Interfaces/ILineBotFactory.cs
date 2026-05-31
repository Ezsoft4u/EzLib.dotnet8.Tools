namespace EzLib.Services
{
    /// <summary>
    /// 依 bot 名稱取得 LINE Messaging API 服務。
    /// </summary>
    public interface ILineBotFactory
    {
        /// <summary>
        /// 取得指定 bot 的 LINE 服務。
        /// </summary>
        ILineService GetBot(string botName);
    }
}
