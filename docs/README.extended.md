# EzLib.dotnet8.Tools (Extended)

本檔案為整合版 README，包含原始內容與新增的 Mail / SMS (中華電信 IMSP) 使用說明，以利後續挑選最終公開版本。

---
## 原始簡介
EzLib.dotnet8.Tools 是一個基於 .NET 8 的庫，提供了一些常用的功能和工具。

### 安裝 (原始版)
```bash
dotnet add package EzLib.dotnet8.Tools
```
或在 `.csproj`：
```xml
<PackageReference Include="EzLib.dotnet8.Tools" Version="<最新版>" />
```

### 原始 Serilog 範例
```csharp
using Serilog; 
using EzLib;

var logConfig = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day);

var logger = new EzLogger(logConfig);
logger.Information("這是一條資訊日誌");
logger.Warning("這是一條警告日誌");
logger.Error("這是一條錯誤日誌");
```

---
## 擴充內容總覽
新增：
- MailService（多收件人 / CC / BCC / 附件 / Debug 詳細 SMTP 紀錄）
- SMS 簡訊服務整合（中華電信 IMSP 平台示例）
- Serilog 整合最佳實務
- 版本與功能清單

---
## 快速開始 (Mail 擴充版)
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
    Debug = true        // 啟用後會輸出 SMTP 詳細流程 (Serilog Debug)
};

var mailer = new SmtpMailer(settings);
var req = new MailRequest
{
    ToEmail = "user1@xxx.com; user2@xxx.com, user3@xxx.com", // 支援分號 / 逗號 / 換行
    Subject = "系統通知",
    Body = "<b>服務執行成功</b>",
    IsHtml = true,
    Cc = "qa@xxx.com",
    Bcc = "audit@xxx.com"
};
var result = await mailer.SendAsync(req);
```

### MailService 特色
| 功能 | 說明 |
|------|------|
| 多收件人 | `,`、`;`、換行分隔都可 |
| 安全測試 | `#if DEBUG` 編譯時自動覆寫收件人避免誤寄 |
| Debug 紀錄 | 連線 / 驗證 / 傳送 / 附件詳細過程 |
| 附件 | 支援 `IFormFile` 集合 |
| 主旨預設 | `[DisplayName] yyyy/MM/dd HH:mm` |
| 錯誤詳情 | 彙整 InnerException 於回傳訊息 |

### SSL 模式 (MailSettings.SSL)
| 值 | 模式 |
|----|------|
| 0  | None |
| 1  | Auto |
| 2  | SslOnConnect |
| 3  | StartTls |
| 4  | StartTlsWhenAvailable |

---
## SMS 簡訊服務（中華電信 IMSP）
示例展示如何透過 DI 註冊並發送簡訊。

### 範例設定檔 `smssettings.json`
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
> 實際欄位請依你專案的 `SmsSettings` 定義。

### 程式使用
```csharp
using EzLib.Extensions;
using EzLib.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

var services = new ServiceCollection();
var cfg = JsonSerializer.Deserialize<SmsSettings>(File.ReadAllText("smssettings.json"));

services.AddSmsService(o => {
    o.ApiServer    = cfg.ApiServer;
    o.ApiPort      = cfg.ApiPort;
    o.ApiRoute     = cfg.ApiRoute;
    o.SenderNumber = cfg.SenderNumber;
    o.Username     = cfg.Username;
    o.Password     = cfg.Password;
});

var provider = services.BuildServiceProvider();
var sms = provider.GetRequiredService<ISmsService>();
var rst = await sms.SendSmsAsync("0987654321", "【測試】IMSP 短訊發送成功");
Console.WriteLine(rst.IsSuccess ? $"成功 ID={rst.MessageId}" : $"失敗: {rst.Message}");
```

### SmsResult 欄位
| 屬性 | 說明 |
|------|------|
| IsSuccess | 是否發送成功 |
| Message | 平台或錯誤訊息 |
| MessageId | 成功回傳訊息 ID |
| RequestBody | （除錯用）送出原文 |
| ResponseBody | （除錯用）平台回應原文 |

### 注意事項
- IMSP 可能需來源 IP 白名單與啟用權限
- 建議使用 Serilog 結構化紀錄失敗案例（避免紀錄敏感帳密）
- 大量發送加入重試與節流（Polly）

---
## Serilog 整合建議
```csharp
using Serilog;
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```
Mail / SMS 服務共用全域 `Log`。

---
## 功能特性 (合併)
- [x] Serilog 整合與檔案/主控台輸出
- [x] 郵件寄送（多收件人 / CC / BCC / 附件 / Debug 詳細紀錄）
- [x] 簡訊服務 DI 擴充（中華電信 IMSP 示例）
- [x] 失敗錯誤訊息包含 InnerException
- [x] Debug 模式安全覆寫收件人
- [ ] 重試/節流策略樣板（規劃中）

---
## 版本歷史 (摘要)
| 版本 | 說明 |
|------|------|
| 1.0.x | 初始：Serilog + Mail + SMS DI |

---
## 依賴項 (合併)
- MailKit / MimeKit
- Serilog / Serilog.AspNetCore / Console / File / MSSqlServer Sinks
- Microsoft.AspNetCore.Http.Features (IFormFile 支援)

---
## 貢獻
歡迎 Issue / PR。

## 授權
MIT 授權，詳見 LICENSE。

---
本檔案為擴充版；如需回退原始精簡版仍可參考 `docs/readme.md` 目前內容。