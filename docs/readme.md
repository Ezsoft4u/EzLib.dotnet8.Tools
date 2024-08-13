# EzLib.dotnet8.Tools

EzLib.dotnet8.Tools 是一個基於 .NET 8 的庫，提供了一些常用的功能和工具。

## 目錄

- [安裝](#安裝)
- [使用](#使用)
- [依賴項](#依賴項)
- [貢獻](#貢獻)
- [授權](#授權)

## 安裝

你可以通過 NuGet 安裝 EzLib：

```bash
dotnet add package EzLib.dotnet8.Tools 
```	
	
或者在你的 `.csproj` 文件中添加以下引用


## 使用

以下是一些使用範例：

### 設定 Serilog

```csharp

using Serilog; using EzLib;
var logConfig = new LoggerConfiguration() 
	.WriteTo.Console() 
	.WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day);

var logger = new EzLogger(logConfig);

logger.Information("這是一條資訊日誌"); 

logger.Warning("這是一條警告日誌"); 

logger.Error("這是一條錯誤日誌");
```

## 依賴項

EzLib 依賴以下 NuGet 包：

- [MailKit](https://www.nuget.org/packages/MailKit) (版本 4.7.1.1)
- [Microsoft.AspNetCore.Http.Features](https://www.nuget.org/packages/Microsoft.AspNetCore.Http.Features) (版本 5.0.17)
- [MimeKit](https://www.nuget.org/packages/MimeKit) (版本 4.7.1)
- [Serilog](https://www.nuget.org/packages/Serilog) (版本 4.0.1)
- [Serilog.AspNetCore](https://www.nuget.org/packages/Serilog.AspNetCore) (版本 8.0.2)
- [Serilog.Sinks.Console](https://www.nuget.org/packages/Serilog.Sinks.Console) (版本 6.0.0)
- [Serilog.Sinks.File](https://www.nuget.org/packages/Serilog.Sinks.File) (版本 6.0.0)
- [Serilog.Sinks.MSSqlServer](https://www.nuget.org/packages/Serilog.Sinks.MSSqlServer) (版本 6.6.1)

## 貢獻

歡迎貢獻！

## 授權

這個專案使用 MIT 授權。詳情請參閱 [LICENSE](LICENSE) 文件。
