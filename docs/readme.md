# EzLib.dotnet8.Tools

EzLib.dotnet8.Tools 是一個基於 .NET 8 的常用工具程式庫，整合：
- Serilog 日誌封裝與快速啟用
- 郵件寄送 (MailKit / MimeKit)
- 簡訊服務擴充 (中華電信 IMSP 平台示例)

## 目錄
- [安裝](#安裝)
- [快速開始 (Mail)](#快速開始-mail)
- [MailService 使用說明](#mailservice-使用說明)
- [SMS 簡訊服務（中華電信 IMSP）](#sms-簡訊服務中華電信-imsp)
- [Serilog 整合](#serilog-整合)
- [功能特性](#功能特性)
- [版本歷史](#版本歷史)
- [依賴項](#依賴項)
- [貢獻](#貢獻)
- [授權](#授權)

## 安裝
```bash
dotnet add package EzLib.dotnet8.Tools
```
或在 `.csproj`：
```xml
<PackageReference Include="EzLib.dotnet8.Tools" Version="<最新版>" />
```

## 快速開始 (Mail)
```csharp
using EzLib;
using EzLib.Models;

var settings = new MailSettings
{
    Mail = "no-reply@yourdomain.com",
    DisplayName = "通知服務",
    Password = "yourpassword",
    Host = "smtp.yourdomain.com",
    Port = 587,
    SSL = 3,            // StartTls
    IsAuth = true,
    Debug = true        // 顯示詳細 SMTP 流程 (Serilog Debug)
};

var mailer = new SmtpMailer(settings);
var request = new MailRequest
{
    ToEmail = "user1@xxx.com; user2@xxx.com", // 支援 ; , 或換行
    Subject = "系統通知",
    Body = "<b>服務執行成功</b>",
    IsHtml = true
};
var result = await mailer.SendAsync(request);
```

## MailService 使用說明
特色：
- 多收件人 / CC / BCC：以 `,`、`;` 或換行分隔
- Debug = true：Serilog Debug 層級紀錄 連線 / 驗證 / 傳送 / 附件
- `#if DEBUG` 編譯時自動覆寫收件人避免誤寄
- 預設主旨：`[DisplayName] yyyy/MM/dd HH:mm`
- 附件支援 `IFormFile`
- 錯誤訊息整合 InnerException

### SSL 模式 (MailSettings.SSL)
| 值 | 模式 |
|----|------|
| 0  | None |
| 1  | Auto |
| 2  | SslOnConnect |
| 3  | StartTls |
| 4  | StartTlsWhenAvailable |

## SMS 簡訊服務（中華電信 IMSP）
提供簡訊服務 DI 擴充，示例對接中華電信 IMSP 平台。

### 範例設定 `smssettings.json`
```json
{
  "ApiServer": "imsp.hinet.net",
  "ApiPort": 443,
  "ApiRoute": "/imsp/sms/api/send",
  "SenderNumber": "0912345678",
  "Username": "your-imsp-account",
  "Password": "your-imsp-password"
}
```
> 實際欄位需符合你的 `SmsSettings` 實作。

### DI 註冊與發送
```csharp
using EzLib.Extensions;
using EzLib.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

var services = new ServiceCollection();
var smsCfg = JsonSerializer.Deserialize<SmsSettings>(File.ReadAllText("smssettings.json"));

services.AddSmsService(o => {
    o.ApiServer    = smsCfg.ApiServer;
    o.ApiPort      = smsCfg.ApiPort;
    o.ApiRoute     = smsCfg.ApiRoute;
    o.SenderNumber = smsCfg.SenderNumber;
    o.Username     = smsCfg.Username;
    o.Password     = smsCfg.Password;
});

var sp  = services.BuildServiceProvider();
var sms = sp.GetRequiredService<ISmsService>();
var rtn = await sms.SendSmsAsync("0987654321", "【測試】IMSP 短訊發送成功");
Console.WriteLine(rtn.IsSuccess ? $"成功 ID={rtn.MessageId}" : $"失敗: {rtn.Message}");
```

### SmsResult 欄位
| 屬性 | 說明 |
|------|------|
| IsSuccess | 是否成功 |
| Message | 平台或錯誤訊息 |
| MessageId | 成功回傳訊息 ID |
| RequestBody | （除錯）送出原文 |
| ResponseBody | （除錯）回應原文 |

### 注意事項
- IMSP 可能需來源 IP 白名單與帳號啟用
- 建議於 Serilog 記錄失敗案例之 Request/Response（避免敏感資訊）
- 大量發送請加入重試 / 節流（Polly）

## Serilog 整合
```csharp
using Serilog;
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

## 功能特性
- [x] Serilog 統一整合
- [x] 郵件寄送（多收件人 / 附件 / Debug）
- [x] 簡訊服務 DI 擴充（IMSP 示例）
- [x] InnerException 錯誤訊息彙整
- [x] Debug 安全收件人覆寫
- [ ] 併發節流與重試策略範本（規劃）

## 版本歷史
| 版本 | 說明 |
|------|------|
| 1.0.x | 郵件 + 簡訊 + Serilog 基礎整合 |

## 依賴項
- MailKit / MimeKit
- Serilog (Console / File / MSSqlServer Sinks)
- Microsoft.AspNetCore.Http.Features

## 貢獻
歡迎 Issue / PR。

## 授權
MIT 授權，詳見 LICENSE。
