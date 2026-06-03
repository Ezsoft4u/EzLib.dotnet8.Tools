# Log Viewer

Log Viewer 提供宿主 ASP.NET Core 專案可掛載的 log 瀏覽 UI 與 API。它讀取的是引用 EzLib 的專案所設定的 log 目錄，不是 EzLib 套件自己的 log。

## 快速開始

在既有 `appsettings.json` 加上宿主專案的系統 log 目錄：

```json
{
  "SystemLogDirectory": "logs"
}
```

`Program.cs`：

```csharp
using EzLib.Extensions;

builder.Services.AddLogViewer(builder.Configuration, "SystemLogDirectory");

var app = builder.Build();

app.UseLogViewer(); // 預設掛載 /logs
```

也可以在 Startup / Program.cs 直接把 appsettings 的值傳進來：

```csharp
builder.Services.AddLogViewer(builder.Configuration["SystemLogDirectory"] ?? "logs");
app.UseLogViewer();
```

`LogDirectory` 可以是絕對路徑，也可以是相對於網站啟動目錄的相對路徑。

## 進階設定

如果要調整掛載路徑、檔名樣式、讀取行數或 Viewer Key，也可以使用 `LogViewer` section：

```json
{
  "LogViewer": {
    "ViewerPath": "/logs",
    "LogDirectory": "logs",
    "FileSearchPattern": "*.txt",
    "DefaultTailLines": 500,
    "MaxTailLines": 5000,
    "RequireViewerKey": true,
    "ViewerKey": "use-environment-or-secret-store"
  }
}
```

請不要把 `ViewerKey` 或其他秘密寫進版控檔案，正式環境建議從環境變數或 secret store 注入。

## 程式碼設定

```csharp
builder.Services.AddLogViewer(options =>
{
    options.ViewerPath = "/logs";
    options.LogDirectory = @"D:\WebApps\MySite\logs";
    options.FileSearchPattern = "log-*.txt";
    options.RequireViewerKey = true;
    options.ViewerKey = builder.Configuration["LOG_VIEWER_KEY"];
});

app.UseLogViewer();
```

## 功能

- 左側列出多個 log 檔，依最後修改時間由新到舊排序。
- 可選擇指定檔案查詢。
- 支援日期時間起迄查詢；Serilog 預設格式的多行 exception stack trace 會跟著同一筆 log 一起顯示。
- 預設每 5 秒重新讀取目前檔案，方便即時看錯誤訊息。
- 僅允許讀取設定目錄底下的檔名，阻擋 `../` 這類路徑跳脫。

## API

| 路徑 | 說明 |
|---|---|
| `GET /logs` | Log Viewer HTML UI |
| `GET /logs/api/files` | 回傳可選擇的 log 檔清單 |
| `GET /logs/api/content?file=log-20260603.txt&from=2026-06-03T00:00:00Z&to=2026-06-03T23:59:59Z&tailLines=500` | 讀取指定 log 檔內容 |

啟用 `RequireViewerKey` 後，每個請求須帶：

```text
X-Log-Viewer-Key: your-key
```
