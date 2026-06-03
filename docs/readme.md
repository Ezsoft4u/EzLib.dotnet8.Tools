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
- [Log Viewer](#log-viewer)
- [系統監控模組 (SystemMonitor)](#系統監控模組-systemmonitor)
- [遠端監控收集模組 (MonitorCollector)](#遠端監控收集模組-monitorcollector)
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

## Log Viewer

Log Viewer 提供 ASP.NET Core 專案可掛載的 log 瀏覽 UI 與 API，讀取的是引用 EzLib 的宿主專案所設定的 log 目錄。

```csharp
using EzLib.Extensions;

builder.Services.AddLogViewer(builder.Configuration, "SystemLogDirectory");
app.UseLogViewer(); // 預設 /logs
```

`appsettings.json`：

```json
{
  "SystemLogDirectory": "logs"
}
```

也可在 Startup / Program.cs 直接傳入 `builder.Configuration["SystemLogDirectory"]`。功能包含多 log 檔選擇、日期時間起訖查詢、5 秒即時刷新，以及多行 exception stack trace 保留。詳細說明見 `docs/log-viewer.md`。

## 功能特性
- [x] Serilog 統一整合
- [x] Log Viewer（宿主專案 log 檔瀏覽 + 起訖時間查詢）
- [x] 郵件寄送（多收件人 / 附件 / Debug）
- [x] 簡訊服務 DI 擴充（IMSP 示例）
- [x] InnerException 錯誤訊息彙整
- [x] Debug 安全收件人覆寫
- [x] 系統監控 API（CPU / RAM / GC / Disk / DB / HTTP）
- [x] 遠端多主機監控收集（MonitorCollector）
- [x] 內建 Web Dashboard（Chart.js 圖表）
- [x] SQLite 本地資料庫支援（零設定）
- [x] SQL Server 資料庫支援
- [ ] 併發節流與重試策略範本（規劃）

## 版本歷史
| 版本 | 說明 |
|------|------|
| 1.0.x | 郵件 + 簡訊 + Serilog 基礎整合 |
| 1.1.x | SystemMonitor（本機監控 API）|
| 1.2.x | MonitorCollector（遠端多主機收集 + Dashboard + SQLite 支援）|

## 依賴項
- MailKit / MimeKit
- Serilog (Console / File / MSSqlServer Sinks)
- Microsoft.AspNetCore.Http.Features
- Microsoft.Data.SqlClient（MonitorCollector SQL Server 後端）
- Microsoft.Data.Sqlite（MonitorCollector SQLite 後端）

## 貢獻
歡迎 Issue / PR。

## 授權
MIT 授權，詳見 LICENSE。

## 系統監控模組 (SystemMonitor)

### 功能概覽

SystemMonitor 採 **pull-based（拉取式）** 設計，只有 API 被呼叫時才即時收集資料，**不啟動任何背景執行緒，不佔用系統資源**。

監控項目包含：
- **OverallStatus** — 整體健康狀態（Healthy / Degraded / Unhealthy）
- **App** — 應用程式名稱、版本、環境、啟動時間
- **System** — 機器名稱、OS、.NET 版本、CPU 核心數
- **SystemRam** — 系統實體記憶體總量、可用量、使用率
- **Process** — Process 記憶體、執行緒數量、運行時間
- **Gc** — GC Heap 大小、Gen0/1/2 回收次數
- **Cpu** — CPU 使用率（%）
- **Disks** — 磁碟空間、使用率、告警狀態
- **Databases** — SQL Server 連線健康檢查（含快取）
- **Http** — 外部 HTTP 端點健康檢查（含快取）

### 安裝

```bash
dotnet add package EzLib.dotnet8.Tools
```

### 快速開始

在 `Program.cs` 加入以下兩行即可，**不需要撰寫任何 Controller**：

```csharp
builder.Services.AddSystemMonitor();
app.MapSystemMonitor(); // 預設掛在 GET /sys/info
```

### 完整設定（程式碼方式）

```csharp
builder.Services.AddSystemMonitor(opt =>
{
    opt.EndpointPath = "/api/sys/info";  // 自訂 endpoint 路徑
    opt.RequireApiKey = true;             // 啟用 API Key 驗證
    opt.ApiKey = "your-secret-key";       // 請求須帶 Header: X-Api-Key: your-secret-key
    opt.EnvironmentName = "Production";   // 留空則自動讀取 ASPNETCORE_ENVIRONMENT

    // 磁碟監控
    opt.Disk.MonitorDrives = ["C:\\", "D:\\"];  // 留空則自動監控所有固定磁碟
    opt.Disk.WarnIfUsedPercentOver = 85;
    opt.Disk.CriticalIfUsedPercentOver = 95;

    // SQL Server 健康檢查（可加多個）
    opt.Database.AddSqlServer("MainDB", "Server=localhost;Database=MyApp;Integrated Security=true;");
    opt.Database.AddSqlServer("LogDB", "Server=localhost;Database=Logs;Integrated Security=true;", cacheSeconds: 10);

    // HTTP 端點健康檢查（可加多個）
    opt.Http.AddEndpoint("PaymentAPI", "https://payment.example.com/health");
    opt.Http.AddEndpoint("NotifyAPI", "https://notify.example.com/ping", timeoutSeconds: 5);
});

app.MapSystemMonitor();
```

### 從 appsettings.json 設定

```csharp
// Program.cs — 一行搞定
builder.Services.AddSystemMonitor(builder.Configuration);
app.MapSystemMonitor();
```

`appsettings.json`：

```json
{
  "SystemMonitor": {
    "EndpointPath": "/sys/info",
    "RequireApiKey": false,
    "ApiKey": null,
    "EnvironmentName": "Production",
    "Disk": {
      "MonitorDrives": ["C:\\"],
      "WarnIfUsedPercentOver": 85,
      "CriticalIfUsedPercentOver": 95
    },
    "Database": {
      "Connections": [
        {
          "Name": "MainDB",
          "ConnectionString": "Server=localhost;Database=MyApp;Integrated Security=true;",
          "Provider": "SqlServer",
          "CacheSeconds": 5
        }
      ]
    },
    "Http": {
      "Endpoints": [
        {
          "Name": "PaymentAPI",
          "Url": "https://payment.example.com/health",
          "TimeoutSeconds": 10,
          "CacheSeconds": 5
        }
      ]
    }
  }
}
```

### API 回傳格式

`GET /sys/info` 回傳 JSON：

```json
{
  "overallStatus": "Healthy",
  "collectedAt": "2026-03-24T10:00:00Z",
  "app": {
    "appName": "MyApp",
    "appVersion": "1.0.0.0",
    "environment": "Production",
    "startedAt": "2026-03-24T09:00:00Z"
  },
  "system": {
    "machineName": "SERVER01",
    "osDescription": "Microsoft Windows 10.0.22631",
    "dotnetVersion": "8.0.14",
    "processorCount": 8
  },
  "systemRam": {
    "totalMB": 16384,
    "availableMB": 8192,
    "usedMB": 8192,
    "usedPercent": 50.0
  },
  "process": {
    "uptimeSeconds": 3600.0,
    "workingSetMB": 128.5,
    "privateMemoryMB": 95.2,
    "threadCount": 24
  },
  "gc": {
    "totalHeapMB": 45.3,
    "gen0Collections": 120,
    "gen1Collections": 30,
    "gen2Collections": 5
  },
  "cpu": {
    "usagePercent": 12.5
  },
  "disks": [
    {
      "drive": "C:\\",
      "driveType": "Fixed",
      "totalGB": 500.0,
      "usedGB": 320.0,
      "freeGB": 180.0,
      "usedPercent": 64.0,
      "status": "OK"
    }
  ],
  "databases": [
    {
      "name": "MainDB",
      "isAlive": true,
      "responseMs": 8,
      "errorMessage": null,
      "isCached": false
    }
  ],
  "http": [
    {
      "name": "PaymentAPI",
      "url": "https://payment.example.com/health",
      "isAlive": true,
      "statusCode": 200,
      "responseMs": 45,
      "errorMessage": null,
      "isCached": false
    }
  ]
}
```

### OverallStatus 判斷邏輯

| 狀態 | 觸發條件 |
|------|---------|
| `Unhealthy` | 任何 Database `isAlive: false`，或任何 Http 端點 `isAlive: false`，或任何磁碟 `status: Critical` |
| `Degraded` | 任何磁碟 `status: Warning`，或 CPU 使用率 > 90% |
| `Healthy` | 以上皆無 |

### API Key 驗證

啟用後，每個請求須帶以下 Header：

```
X-Api-Key: your-secret-key
```

未帶或錯誤時回傳 `401 Unauthorized`。

### 注意事項

- `SystemInfoService` 以 **Singleton** 方式注入，CPU delta 計算的靜態快取才能正常運作
- DB / HTTP 健康檢查有快取（預設 5 秒），快取期間不重新查詢，`isCached: true` 表示回傳快取值
- CPU 使用率第一次呼叫固定回傳 `0`，第二次起才有準確數值
- System RAM 在 Windows 使用 `GlobalMemoryStatusEx`，Linux 讀取 `/proc/meminfo`

---

## 遠端監控收集模組 (MonitorCollector)

### 功能概覽

MonitorCollector 是 **集中式遠端監控** 模組，定期從多台已部署 SystemMonitor 的主機拉取資料，儲存至資料庫，並提供內建 Web Dashboard 進行視覺化展示。

```
[ 主機 A /sys/info ]  ─┐
[ 主機 B /sys/info ]  ─┤──▶  MonitorCollectorService  ──▶  DB  ──▶  /monitor Dashboard
[ 主機 C /sys/info ]  ─┘         (PeriodicTimer)           ↑ SQL Server 或 SQLite
```

**主要功能：**
- 定時（可設定間隔）並行從多台主機 Pull 資料
- 自動建立資料表（無需手動 Migration）
- 支援 **SQLite**（本地檔案，零設定）與 **SQL Server**（生產環境）
- 內建 Dashboard（Chart.js 折線圖 + KPI 卡片 + 磁碟 / DB / HTTP 狀態表）
- 資料保留天數自動清理
- Dashboard 可選 API Key 保護

### 快速開始（SQLite 本地，零設定）

**Step 1**：在「被監控主機」啟用 SystemMonitor API：

```csharp
// 被監控主機的 Program.cs
builder.Services.AddSystemMonitor();
app.MapSystemMonitor(); // 暴露 GET /sys/info
```

**Step 2**：在「收集主機」（可以是同一台或另一台）加入 MonitorCollector：

```csharp
// 收集主機的 Program.cs
builder.Services.AddMonitorCollector(opt =>
{
    opt.Provider                = MonitorStorageProvider.Sqlite;
    opt.StorageConnectionString = "monitor.db";   // 自動在執行目錄建立 SQLite 檔案
    opt.CollectIntervalSeconds  = 60;              // 每 60 秒收集一次
    opt.RetentionDays           = 30;              // 保留 30 天資料
    opt.Hosts.Add(new MonitorHostSettings
    {
        Name = "WebServer1",
        Url  = "https://server1.example.com/sys/info"
    });
    opt.Hosts.Add(new MonitorHostSettings
    {
        Name           = "ApiServer",
        Url            = "https://api.example.com/sys/info",
        ApiKey         = "server-api-key",   // 若目標主機有啟用 RequireApiKey
        TimeoutSeconds = 10
    });
});

app.UseMonitorDashboard(); // 掛載 Dashboard，預設路徑 /monitor
```

開啟瀏覽器前往 `https://your-host/monitor` 即可看到 Dashboard。

### 完整設定（程式碼方式）

```csharp
builder.Services.AddMonitorCollector(opt =>
{
    // ── 資料庫 ────────────────────────────────────────────
    opt.Provider                = MonitorStorageProvider.SqlServer; // 或 Sqlite
    opt.StorageConnectionString = "Server=localhost;Database=Monitor;Integrated Security=true;";

    // ── 收集行為 ──────────────────────────────────────────
    opt.CollectIntervalSeconds = 60;   // 收集間隔（秒）
    opt.RetentionDays          = 30;   // 資料保留天數（0 = 永久保留）

    // ── Dashboard ─────────────────────────────────────────
    opt.DashboardPath        = "/monitor";         // Dashboard 掛載路徑
    opt.RequireDashboardKey  = true;               // 是否需要 API Key
    opt.DashboardKey         = "your-secret-key";  // Header: X-Monitor-Key

    // ── 被監控主機清單 ─────────────────────────────────────
    opt.Hosts.Add(new MonitorHostSettings
    {
        Name           = "WebServer1",
        Url            = "https://server1.example.com/sys/info",
        TimeoutSeconds = 15
    });
    opt.Hosts.Add(new MonitorHostSettings
    {
        Name           = "ApiServer",
        Url            = "https://api.example.com/sys/info",
        ApiKey         = "api-server-key",
        TimeoutSeconds = 10
    });
});

app.UseMonitorDashboard();
```

### 從 appsettings.json 設定

```csharp
// Program.cs — 兩行搞定
builder.Services.AddMonitorCollector(builder.Configuration);
app.UseMonitorDashboard();
```

`appsettings.json`：

```json
{
  "MonitorCollector": {
    "Provider": "Sqlite",
    "StorageConnectionString": "monitor.db",
    "CollectIntervalSeconds": 60,
    "RetentionDays": 30,
    "DashboardPath": "/monitor",
    "RequireDashboardKey": false,
    "DashboardKey": null,
    "Hosts": [
      {
        "Name": "WebServer1",
        "Url": "https://server1.example.com/sys/info",
        "TimeoutSeconds": 15
      },
      {
        "Name": "ApiServer",
        "Url": "https://api.example.com/sys/info",
        "ApiKey": "api-server-key",
        "TimeoutSeconds": 10
      }
    ]
  }
}
```

### 設定參數說明

#### MonitorCollectorSettings

| 屬性 | 型別 | 預設值 | 說明 |
|------|------|--------|------|
| `Provider` | `MonitorStorageProvider` | `SqlServer` | 儲存後端：`SqlServer` 或 `Sqlite` |
| `StorageConnectionString` | `string` | `""` | SQL Server 連線字串，或 SQLite 檔案路徑（如 `"monitor.db"`）|
| `CollectIntervalSeconds` | `int` | `60` | 收集間隔秒數 |
| `RetentionDays` | `int` | `30` | 資料保留天數（`0` 表示永久保留）|
| `DashboardPath` | `string` | `"/monitor"` | Dashboard UI 掛載路徑 |
| `RequireDashboardKey` | `bool` | `false` | 是否啟用 Dashboard API Key 驗證 |
| `DashboardKey` | `string?` | `null` | Dashboard API Key 值 |
| `Hosts` | `List<MonitorHostSettings>` | `[]` | 被監控主機清單 |

#### MonitorHostSettings

| 屬性 | 型別 | 預設值 | 說明 |
|------|------|--------|------|
| `Name` | `string` | `""` | 主機識別名稱（顯示於 Dashboard 側邊欄）|
| `Url` | `string` | `""` | SystemMonitor API 完整 URL（如 `https://host/sys/info`）|
| `ApiKey` | `string?` | `null` | 目標主機的 API Key（若該主機啟用了 `RequireApiKey`）|
| `TimeoutSeconds` | `int` | `15` | HTTP 請求逾時秒數 |

#### 儲存後端比較

| | SQLite | SQL Server |
|---|---|---|
| 需要伺服器 | ❌ 不需要 | ✅ 需要 |
| 儲存位置 | 本地 `.db` 檔案 | 遠端 / 本地 SQL Server |
| 適合情境 | 開發、單機、小規模 | 生產、多機、大流量 |
| 設定複雜度 | 極低，填檔案路徑即可 | 需提供完整連線字串 |

### Dashboard 功能

| 功能 | 說明 |
|------|------|
| 左側主機列表 | 自動列出資料庫中所有主機，點擊切換 |
| 時間範圍選擇 | 最近 1h / 6h / 24h / 7 天 / 自訂區間 |
| KPI 卡片 | CPU%、RAM%、Process 記憶體、Thread 數、Disk / DB / HTTP 狀態 |
| 折線圖 | CPU 使用率、系統 RAM 使用率、Process 記憶體歷史趨勢（Chart.js）|
| 磁碟狀態表 | 磁碟路徑、類型、總量 / 已用 / 剩餘 / 使用率 / 狀態 |
| 資料庫狀態表 | 名稱、連線狀態、回應時間、是否快取 |
| HTTP 端點狀態表 | 名稱、URL、狀態、HTTP Code、回應時間 |
| CSV 匯出 | 匯出目前時間範圍的快照資料 |
| 自動刷新 | 每 60 秒自動重新載入資料 |

### Dashboard API 端點

| 路徑 | 說明 |
|------|------|
| `GET /monitor` | Dashboard HTML UI |
| `GET /monitor/api/hosts` | 回傳所有主機名稱清單（JSON）|
| `GET /monitor/api/snapshots?host=&from=&to=` | 查詢快照資料（JSON）|
| `GET /monitor/api/export?host=&from=&to=` | 匯出 CSV 檔案 |

> `from` / `to` 參數格式為 ISO 8601（如 `2026-01-01T00:00:00Z`）

### Dashboard API Key 保護

啟用後，所有 Dashboard 請求（包含 API）須帶以下 Header：

```
X-Monitor-Key: your-secret-key
```

未帶或錯誤時回傳 `401 Unauthorized`。

### 注意事項

- `AddMonitorCollector` 會自動呼叫 `EnsureTableAsync()` 建立資料表，無需手動執行 Migration
- SQLite 檔案路徑為相對路徑時，以應用程式工作目錄為基準
- `CollectIntervalSeconds` 最小建議 `30`，過短可能造成被監控主機 CPU 負擔
- 各主機 Pull 操作為**平行執行**（`Task.WhenAll`），單台失敗不影響其他主機
- 資料清理排程固定為每 24 小時執行一次，`RetentionDays = 0` 表示永久保留不清理
