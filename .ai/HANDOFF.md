---
title: EzLib Handoff
type: handoff
status: active
project: Ezsoft4u-EzLib.dotnet8.Tools
updated: 2026-06-05
tags:
  - ai-team
  - handoff
---

# Handoff

## Current Focus

Dependency metadata alignment shipped in EzLib NuGet package `1.0.21`.

## What Changed Recently

- Repo-local `.ai/` memory entrypoints were created for project `Ezsoft4u-EzLib.dotnet8.Tools`.
- LINE Messaging API support was added:
  - `LineSettings`, `LineSendResult`, and webhook models.
  - `ILineService` / `LineService` for text push, text reply, webhook signature validation, and webhook parsing.
  - `LineWebhookEndpoint.HandleAsync` and `MapLineWebhook(...)` for basic minimal API webhook receiving.
  - `AddLineService(...)` DI extensions.
  - `docs/line-messaging.md` usage documentation.
  - `EzLib.Tests` xUnit test project with 6 LINE tests.
- MailKit and MimeKit were upgraded to 4.16.0 to remove NU1902 vulnerability warnings.
- `MailService` validates password before MailKit authentication when `IsAuth=true`, removing the MailKit nullable warning introduced by the newer API.
- LINE support now supports multiple bots:
  - configure `LineBots:{botName}:ChannelAccessToken/ChannelSecret`
  - resolve `ILineBotFactory.GetBot("botName")`
  - map named webhook routes with `MapLineWebhook("/line/admin/webhook", "admin", handler)`
- Log Viewer support was added for consuming ASP.NET Core projects:
  - `LogViewerSettings`, `LogViewerFile`, `LogReadRequest`, and `LogReadResult`.
  - `ILogViewerService` / `FileLogViewerService` for configured log directory file listing and content reading.
  - Date/time range filtering preserves continuation lines such as exception stack traces.
  - Path traversal file names are rejected.
  - `LogViewerMiddleware` provides `/logs`, `/logs/api/files`, and `/logs/api/content`.
  - `AddLogViewer(...)` and `UseLogViewer()` extension methods.
  - Preferred simple setup reads a single appsettings key such as `"SystemLogDirectory": "logs"` through `builder.Services.AddLogViewer(builder.Configuration, "SystemLogDirectory")`.
  - Browser smoke test found favicon 404 console noise; HTML now includes an inline favicon.
  - `docs/log-viewer.md` and `docs/readme.md` usage notes.
  - Package metadata bumped to `1.0.18` with Log Viewer release notes and `docs/log-viewer.md` included in the nupkg.
- Session started with existing uncommitted changes in:
  - `EzLib.nuspec`
  - `Models/MailServiceVM.cs`
  - `Services/Interfaces/IMailService.cs`
  - `Services/MailService.cs`
  - `SmtpMailer.cs`
- 2026-06-05 dependency alignment:
  - `Azure.Identity` changed to `1.17.2`.
  - `Microsoft.Data.SqlClient` changed to `6.1.5`.
  - `Microsoft.Data.Sqlite` changed to `9.0.16`.
  - `EzLib.csproj` and `EzLib.nuspec` package version bumped to `1.0.21`.
  - Commit `d013198 chore: align package dependency versions` pushed to `main`.
  - NuGet package `EzLib.dotnet8.Tools` version `1.0.21` published and indexed.

## Next Best Action

No immediate next action. `EzLib.dotnet8.Tools` version `1.0.21` is published and indexed.

## Watchouts

- Never commit LINE tokens, channel secrets, connection strings, or customer data.
- AI-added or modified methods should include concise Chinese comments.
- `ServiceCollectionExtensions.cs` is non-UTF-8/legacy encoded, so LINE DI extensions were added in a separate UTF-8 file instead of editing it.
- Existing SMS nullable warnings remain in `SmsService.cs`; MailKit/MimeKit vulnerability warnings are resolved.
- `ApiBaseUrl` is an optional advanced override only; normal users should not configure it.
- Log Viewer reads the host project's configured `LogDirectory`; it does not read EzLib's own package directory.
- Protect `/logs` in deployed systems with `RequireViewerKey` or upstream auth because log files can contain sensitive operational details.

## Open Questions

- None currently.

## Verification

- Temporary ASP.NET Core net8.0 smoke host referenced local EzLib, used only `"SystemLogDirectory": "system-logs"` in appsettings, and served `/logs`.
- HTTP smoke passed for `/logs`, `/logs/api/files`, and `/logs/api/content?file=app-20260603.txt&from=2026-06-03T10:20:00%2B08:00&to=2026-06-03T10:40:00%2B08:00&tailLines=50`.
- Playwright browser smoke loaded `/logs`, listed two log files, and UI date-range query returned only the 10:30 error with stack trace.
- `dotnet test .\EzLib.Tests\EzLib.Tests.csproj -v minimal` passed on 2026-06-03 with 17 tests.
- `dotnet build .\EzLib.csproj -c Release -v minimal` passed on 2026-06-03 with existing `SmsService` nullable warnings.
- `nuget pack .\EzLib.nuspec -OutputDirectory $env:TEMP\EzLibPackCheck-1.0.18` produced `EzLib.dotnet8.Tools.1.0.18.nupkg`; package inspection confirmed `docs/readme.md`, `docs/line-messaging.md`, and `docs/log-viewer.md`.
- `nuget push` returned `Your package was pushed` for `EzLib.dotnet8.Tools.1.0.18.nupkg`.
- `https://www.nuget.org/packages/EzLib.dotnet8.Tools/1.0.18` returned HTTP 200 and page content confirmed package id/version. Search/flat-container endpoints still showed `1.0.17` immediately after push, likely NuGet indexing delay.
- 2026-06-03 dependency correction: `Serilog.AspNetCore` changed from `10.0.0` to `9.0.0`; `dotnet list .\EzLib.csproj package` confirmed requested/resolved `9.0.0`.
- `dotnet test .\EzLib.Tests\EzLib.Tests.csproj -v minimal` passed on 2026-06-03 with 17 tests after the dependency correction.
- `dotnet build .\EzLib.csproj -c Release -v minimal` passed on 2026-06-03 with existing `SmsService` nullable warnings after the dependency correction.
- `nuget pack .\EzLib.nuspec -OutputDirectory $env:TEMP\EzLibPackCheck-1.0.19` produced `EzLib.dotnet8.Tools.1.0.19.nupkg`; package inspection confirmed `Serilog.AspNetCore` dependency version `9.0.0`.
- `nuget push` returned `Your package was pushed` for `EzLib.dotnet8.Tools.1.0.19.nupkg`.
- `https://www.nuget.org/packages/EzLib.dotnet8.Tools/1.0.19` returned HTTP 200 and flat-container index included `1.0.19`.
- 2026-06-03 Log Viewer fix: `AddLogViewer(configuration, "PxApi:LogDirectory")` now supports nested appsettings keys that resolve to absolute paths or relative paths under the host `ContentRootPath`.
- Default `LogViewerSettings.FileSearchPattern` changed from `*.txt` to `*.*` so `.log` files are listed without extra configuration.
- `dotnet test .\EzLib.Tests\EzLib.Tests.csproj -v minimal` passed on 2026-06-03 with 18 tests after the nested key/content-root fix.
- `dotnet build .\EzLib.csproj -c Release -v minimal` passed on 2026-06-03 with existing `SmsService` nullable warnings after the nested key/content-root fix.
- Temporary ASP.NET Core smoke host used `PxApi:LogDirectory = logs`, created `logs/app.log`, and `/logs/api/files` returned `app.log`.
- `nuget pack .\EzLib.nuspec -OutputDirectory $env:TEMP\EzLibPackCheck-1.0.20` produced `EzLib.dotnet8.Tools.1.0.20.nupkg`.
- `nuget push` returned `Your package was pushed` for `EzLib.dotnet8.Tools.1.0.20.nupkg`.
- `https://www.nuget.org/packages/EzLib.dotnet8.Tools/1.0.20` returned HTTP 200 and flat-container index included `1.0.20`.
- 2026-06-05: `dotnet restore .\EzLib.csproj -v minimal` passed.
- 2026-06-05: `dotnet build .\EzLib.csproj -c Release -v minimal` passed with existing `SmsService` nullable warnings.
- 2026-06-05: `dotnet test .\EzLib.Tests\EzLib.Tests.csproj -v minimal` passed with 18 tests.
- 2026-06-05: `nuget pack .\EzLib.nuspec -OutputDirectory $env:TEMP\EzLibPackCheck-1.0.21` produced `EzLib.dotnet8.Tools.1.0.21.nupkg`, and inspected metadata showed the aligned dependency versions.
- 2026-06-05: `nuget push` returned `Your package was pushed` for `EzLib.dotnet8.Tools.1.0.21.nupkg`.
- 2026-06-05: `https://www.nuget.org/packages/EzLib.dotnet8.Tools/1.0.21` returned HTTP 200 and flat-container index included `1.0.21`.
