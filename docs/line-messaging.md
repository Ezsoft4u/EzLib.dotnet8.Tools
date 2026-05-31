# LINE Messaging API

EzLib provides a small LINE Messaging API wrapper for text notifications and webhook receiving.

LINE Notify is not used because the service ended on 2025-03-31. Use a LINE Messaging API channel instead.

## Configuration

```json
{
  "LineSettings": {
    "ChannelAccessToken": "<from secure configuration>",
    "ChannelSecret": "<from secure configuration>"
  }
}
```

Do not store real tokens or channel secrets in source control.

`ApiBaseUrl` defaults to `https://api.line.me`. Most applications should not set it. It exists only for tests, proxies, or special network routing.

## Dependency Injection

```csharp
using EzLib.Extensions;

builder.Services.AddLineService(builder.Configuration);
```

Or configure manually:

```csharp
builder.Services.AddLineService(options =>
{
    options.ChannelAccessToken = builder.Configuration["Line:ChannelAccessToken"];
    options.ChannelSecret = builder.Configuration["Line:ChannelSecret"];
});
```

## Multiple LINE Bots

Use `AddLineBots` when one system has multiple LINE Messaging API channels.

```json
{
  "LineBots": {
    "customer": {
      "ChannelAccessToken": "<customer bot token>",
      "ChannelSecret": "<customer bot secret>"
    },
    "admin": {
      "ChannelAccessToken": "<admin bot token>",
      "ChannelSecret": "<admin bot secret>"
    }
  }
}
```

```csharp
using EzLib.Extensions;
using EzLib.Services;

builder.Services.AddLineBots(builder.Configuration);

var factory = serviceProvider.GetRequiredService<ILineBotFactory>();
var customerLine = factory.GetBot("customer");
await customerLine.PushTextMessageAsync("Uxxxxxxxxxxxxxxxx", "Customer notice");
```

## Push Text Message

```csharp
using EzLib.Services;

var line = serviceProvider.GetRequiredService<ILineService>();
var result = await line.PushTextMessageAsync("Uxxxxxxxxxxxxxxxx", "Hello from EzLib");
```

The `to` value can be a LINE user ID, group ID, or room ID that your bot is allowed to message.

## Raw LINE Messages

Use raw message methods when another service builds LINE payloads such as Flex Messages.

```csharp
await line.PushMessagesAsync(userId, new object[]
{
    new
    {
        type = "flex",
        altText = "approval",
        contents = flexBubble
    }
});

await line.ReplyMessagesAsync(replyToken, messages);
```

## Reply To Webhook Message

```csharp
await line.ReplyTextMessageAsync(webhookEvent.ReplyToken!, "Received");
```

Reply tokens are short-lived and should only be used for the webhook event that provided them.

## Basic Webhook Endpoint

For minimal API projects, EzLib provides a thin endpoint helper.

Single bot:

```csharp
using EzLib.Services;

app.MapLineWebhook("/line/webhook", async (webhookEvent, line, httpContext, cancellationToken) =>
{
    if (webhookEvent.Type == "message" &&
        webhookEvent.Message?.Type == "text" &&
        !string.IsNullOrWhiteSpace(webhookEvent.ReplyToken))
    {
        await line.ReplyTextMessageAsync(
            webhookEvent.ReplyToken,
            $"Echo: {webhookEvent.Message.Text}",
            cancellationToken);
    }
});
```

Multiple bots:

```csharp
app.MapLineWebhook("/line/customer/webhook", "customer", async (webhookEvent, line, httpContext, cancellationToken) =>
{
    await line.ReplyTextMessageAsync(webhookEvent.ReplyToken!, "Customer bot received", cancellationToken);
});

app.MapLineWebhook("/line/admin/webhook", "admin", async (webhookEvent, line, httpContext, cancellationToken) =>
{
    await line.ReplyTextMessageAsync(webhookEvent.ReplyToken!, "Admin bot received", cancellationToken);
});
```

Each LINE channel should set its own webhook URL in LINE Developers. The named webhook helper uses the selected bot's `ChannelSecret` to validate `x-line-signature`, and the handler receives an `ILineService` using the same bot's `ChannelAccessToken`.

The helper reads the raw request body, validates `x-line-signature`, parses events, dispatches each event to your handler, and returns `200 OK`. Invalid signatures return `401 Unauthorized`.

For advanced routing, logging, queueing, retries, or custom error handling, write your own controller and call:

```csharp
line.ValidateWebhookSignature(body, signature);
line.ParseWebhook(body);
```

## First Version Scope

- Text push message
- Text reply message
- Multiple bot factory
- Webhook signature validation
- Basic webhook JSON parsing for common message fields
- Minimal API webhook helper

Rich messages, Flex messages, media download, broadcast, multicast, and rich menu APIs are intentionally not included in this first version.
