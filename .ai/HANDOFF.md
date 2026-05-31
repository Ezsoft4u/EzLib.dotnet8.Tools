---
title: EzLib Handoff
type: handoff
status: active
project: Ezsoft4u-EzLib.dotnet8.Tools
updated: 2026-05-31
tags:
  - ai-team
  - handoff
---

# Handoff

## Current Focus

Add LINE notification API and message receiving API helpers to the EzLib .NET utility library.

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
- Session started with existing uncommitted changes in:
  - `EzLib.nuspec`
  - `Models/MailServiceVM.cs`
  - `Services/Interfaces/IMailService.cs`
  - `Services/MailService.cs`
  - `SmtpMailer.cs`

## Next Best Action

If publishing a NuGet package, decide whether to bump the package version and update release notes for LINE Messaging API support.

## Watchouts

- Never commit LINE tokens, channel secrets, connection strings, or customer data.
- AI-added or modified methods should include concise Chinese comments.
- `ServiceCollectionExtensions.cs` is non-UTF-8/legacy encoded, so LINE DI extensions were added in a separate UTF-8 file instead of editing it.
- Existing SMS nullable warnings remain in `SmsService.cs`; MailKit/MimeKit vulnerability warnings are resolved.
- `ApiBaseUrl` is an optional advanced override only; normal users should not configure it.

## Open Questions

- None currently; implement a minimal reusable LINE Messaging API wrapper with configuration injected by the caller.
