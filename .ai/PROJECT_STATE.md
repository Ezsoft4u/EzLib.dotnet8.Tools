---
title: EzLib Project State
type: project-state
status: active
project: Ezsoft4u-EzLib.dotnet8.Tools
updated: 2026-05-31
tags:
  - ai-team
  - project-state
---

# Project State

## Goal

Shared .NET 8 utility library for Ezsoft4u projects.

## Current State

- Existing library includes mail, SMS, logging, dependency injection extensions, and packaging metadata.
- Working tree had pre-existing uncommitted changes in mail-related files and `EzLib.nuspec` at session start.
- 2026-05-31 request: add LINE notification API support and message-receiving API support.

## Done

- [x] Repo-local `.ai/` memory entrypoints initialized for project `Ezsoft4u-EzLib.dotnet8.Tools`.
- [x] Added LINE Messaging API text push/reply service, webhook signature validation, webhook parsing, DI registration, and basic minimal API endpoint helper.
- [x] Added `EzLib.Tests` coverage for LINE signature validation, push/reply payloads, webhook parsing, and endpoint dispatch.

## Doing

- [ ] Run final build/test verification and close out handoff.

## Next Actions

1. Inspect existing service/model/export patterns.
2. Add LINE models, service interface, implementation, and DI registration following the repo style.
3. Build the project and update handoff.

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

## Commands

```text
dotnet build EzLib.sln
```

## Verification

- `dotnet test .\EzLib.Tests\EzLib.Tests.csproj -v minimal` passed on 2026-05-31 with 6 tests. Existing MailKit/MimeKit vulnerability warnings remain.
- `dotnet build .\EzLib.csproj -c Release -v minimal` passed on 2026-05-31. Existing MailKit/MimeKit vulnerability warnings and SMS nullable warnings remain.
- `nuget pack .\EzLib.nuspec -OutputDirectory $env:TEMP\EzLibPackCheck` passed on 2026-05-31 and produced `EzLib.dotnet8.Tools.1.0.8.nupkg` in temp.

## Follow-up Changes

- 2026-05-31: MailKit and MimeKit upgraded to 4.16.0 to remove NU1902 vulnerability warnings.
- 2026-05-31: `MailService` now validates `MailSettings.Password` before calling MailKit authentication when `IsAuth=true`, removing the MailKit nullable warning after the package upgrade.
- 2026-05-31: LINE support expanded from single bot to multi-bot factory through `LineBotOptions`, `ILineBotFactory`, `LineBotFactory`, `AddLineBots(...)`, and named `MapLineWebhook(..., botName, ...)`.

## Notes For Cloud Agents

- Do not write LINE channel access tokens or channel secrets into repo files.
- Use environment/configuration injection for secrets.
