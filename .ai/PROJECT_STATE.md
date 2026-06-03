---
title: EzLib Project State
type: project-state
status: active
project: Ezsoft4u-EzLib.dotnet8.Tools
updated: 2026-06-03
tags:
  - ai-team
  - project-state
---

# Project State

## Goal

Shared .NET 8 utility library for Ezsoft4u projects.

## Current State

- Existing library includes mail, SMS, logging, log viewer, dependency injection extensions, and packaging metadata.
- Working tree had pre-existing uncommitted changes in mail-related files and `EzLib.nuspec` at session start.
- 2026-05-31 request: add LINE notification API support and message-receiving API support.

## Done

- [x] Repo-local `.ai/` memory entrypoints initialized for project `Ezsoft4u-EzLib.dotnet8.Tools`.
- [x] Added LINE Messaging API text push/reply service, webhook signature validation, webhook parsing, DI registration, and basic minimal API endpoint helper.
- [x] Added `EzLib.Tests` coverage for LINE signature validation, push/reply payloads, webhook parsing, and endpoint dispatch.
- [x] Added Log Viewer middleware and service for host projects to browse their configured log directory from `/logs`.
- [x] Added tests for log file listing, date-range filtering, path traversal rejection, and middleware routing.
- [x] Added simplified Log Viewer registration from a single appsettings key such as `SystemLogDirectory`, plus direct string log directory registration.
- [x] Published NuGet package `EzLib.dotnet8.Tools` version `1.0.18`.
- [x] Corrected `Serilog.AspNetCore` dependency from `10.0.0` to `9.0.0` for the next package.
- [x] Published NuGet package `EzLib.dotnet8.Tools` version `1.0.19`.

## Doing

- [ ] None currently.

## Next Actions

1. Consider whether Log Viewer should be protected by a deployment-specific key in each consuming project.

## Blockers

- None.

## Important Files

| Path | Purpose |
|---|---|
| `EzLib.csproj` | Library project configuration. |
| `Extensions/ServiceCollectionExtensions.cs` | Dependency injection registration helpers. |
| `Services/` | Service implementations. |
| `Services/Interfaces/` | Service contracts. |
| `Models/` | Public models used by services. |
| `docs/line-messaging.md` | LINE Messaging API usage notes. |
| `docs/log-viewer.md` | Log Viewer usage notes. |

## Commands

```text
dotnet build EzLib.sln
```

## Verification

- `dotnet test .\EzLib.Tests\EzLib.Tests.csproj -v minimal` passed on 2026-05-31 with 6 tests. Existing MailKit/MimeKit vulnerability warnings remain.
- `dotnet build .\EzLib.csproj -c Release -v minimal` passed on 2026-05-31. Existing MailKit/MimeKit vulnerability warnings and SMS nullable warnings remain.
- `nuget pack .\EzLib.nuspec -OutputDirectory $env:TEMP\EzLibPackCheck` passed on 2026-05-31 and produced `EzLib.dotnet8.Tools.1.0.8.nupkg` in temp.
- `dotnet test .\EzLib.Tests\EzLib.Tests.csproj -v minimal` passed on 2026-06-03 with 17 tests.
- `dotnet build .\EzLib.csproj -c Release -v minimal` passed on 2026-06-03 with existing `SmsService` nullable warnings.
- 2026-06-03 smoke test: temporary ASP.NET Core net8.0 host referenced local EzLib, configured only `"SystemLogDirectory": "system-logs"`, served `/logs`, `/logs/api/files`, and `/logs/api/content`; Playwright browser UI loaded and date-range query returned the expected error log plus stack trace.

## Follow-up Changes

- 2026-05-31: MailKit and MimeKit upgraded to 4.16.0 to remove NU1902 vulnerability warnings.
- 2026-05-31: `MailService` now validates `MailSettings.Password` before calling MailKit authentication when `IsAuth=true`, removing the MailKit nullable warning after the package upgrade.
- 2026-05-31: LINE support expanded from single bot to multi-bot factory through `LineBotOptions`, `ILineBotFactory`, `LineBotFactory`, `AddLineBots(...)`, and named `MapLineWebhook(..., botName, ...)`.
- 2026-06-03: Log Viewer added through `LogViewerSettings`, `ILogViewerService`, `FileLogViewerService`, `LogViewerMiddleware`, `AddLogViewer(...)`, and `UseLogViewer()` for NuGet version `1.0.18`.
- 2026-06-03: Log Viewer docs now show the preferred simple appsettings usage: `"SystemLogDirectory": "logs"` with `builder.Services.AddLogViewer(builder.Configuration, "SystemLogDirectory")`.
- 2026-06-03: Real browser smoke test found favicon 404 noise; `LogViewerMiddleware` HTML now declares an inline favicon.
- 2026-06-03: Package metadata bumped to `1.0.18`; release notes and NuGet docs include Log Viewer.
- 2026-06-03: `nuget push` for `EzLib.dotnet8.Tools.1.0.18.nupkg` succeeded. NuGet version page returned HTTP 200 and confirmed package id/version; search/flat-container indexing may lag shortly after publish.
- 2026-06-03: Package metadata bumped to `1.0.19`; `Serilog.AspNetCore` pinned to `9.0.0` in both csproj and nuspec.
- 2026-06-03: `nuget push` for `EzLib.dotnet8.Tools.1.0.19.nupkg` succeeded; NuGet version page returned HTTP 200 and flat-container index included `1.0.19`.

## Notes For Cloud Agents

- Do not write LINE channel access tokens or channel secrets into repo files.
- Use environment/configuration injection for secrets.
