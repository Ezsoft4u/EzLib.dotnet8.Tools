using System.Net;
using System.Security.Cryptography;
using System.Text;
using EzLib.Models;
using EzLib.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EzLib.Tests;

public class LineServiceTests
{
    [Fact]
    public void ValidateWebhookSignature_accepts_matching_signature()
    {
        var body = """{"events":[]}""";
        var settings = new LineSettings
        {
            ChannelAccessToken = "token",
            ChannelSecret = "secret"
        };
        var signature = CreateLineSignature(body, settings.ChannelSecret);
        var service = CreateService(settings);

        var isValid = service.ValidateWebhookSignature(body, signature);

        Assert.True(isValid);
    }

    [Fact]
    public async Task PushTextMessageAsync_posts_line_push_payload()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{}""", Encoding.UTF8, "application/json")
            };
        });
        var service = CreateService(new LineSettings
        {
            ChannelAccessToken = "access-token",
            ChannelSecret = "secret"
        }, handler);

        var result = await service.PushTextMessageAsync("U123", "hello");

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("https://api.line.me/v2/bot/message/push", capturedRequest.RequestUri?.ToString());
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("access-token", capturedRequest.Headers.Authorization?.Parameter);

        Assert.Contains("\"to\":\"U123\"", capturedBody);
        Assert.Contains("\"type\":\"text\"", capturedBody);
        Assert.Contains("\"text\":\"hello\"", capturedBody);
    }

    [Fact]
    public async Task ReplyTextMessageAsync_posts_line_reply_payload()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{}""", Encoding.UTF8, "application/json")
            };
        });
        var service = CreateService(new LineSettings
        {
            ChannelAccessToken = "access-token",
            ChannelSecret = "secret"
        }, handler);

        var result = await service.ReplyTextMessageAsync("reply-token", "received");

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("https://api.line.me/v2/bot/message/reply", capturedRequest.RequestUri?.ToString());

        Assert.Contains("\"replyToken\":\"reply-token\"", capturedBody);
        Assert.Contains("\"text\":\"received\"", capturedBody);
    }

    [Fact]
    public async Task PushMessagesAsync_posts_raw_line_messages()
    {
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{}""", Encoding.UTF8, "application/json")
            };
        });
        var service = CreateService(new LineSettings
        {
            ChannelAccessToken = "access-token",
            ChannelSecret = "secret"
        }, handler);
        var messages = new object[]
        {
            new
            {
                type = "flex",
                altText = "approval",
                contents = new
                {
                    type = "bubble",
                    body = new
                    {
                        type = "box",
                        layout = "vertical",
                        contents = Array.Empty<object>()
                    }
                }
            }
        };

        var result = await service.PushMessagesAsync("U123", messages);

        Assert.True(result.IsSuccess);
        Assert.Contains("\"to\":\"U123\"", capturedBody);
        Assert.Contains("\"type\":\"flex\"", capturedBody);
        Assert.Contains("\"altText\":\"approval\"", capturedBody);
    }

    [Fact]
    public void ParseWebhook_parses_text_message_event()
    {
        var service = CreateService();
        var body = """
        {
          "destination": "Udeadbeef",
          "events": [
            {
              "type": "message",
              "replyToken": "reply-token",
              "timestamp": 1462629479859,
              "source": { "type": "user", "userId": "U123" },
              "message": { "id": "444573844083572737", "type": "text", "text": "hi" }
            }
          ]
        }
        """;

        var request = service.ParseWebhook(body);

        var webhookEvent = Assert.Single(request.Events);
        Assert.Equal("message", webhookEvent.Type);
        Assert.Equal("reply-token", webhookEvent.ReplyToken);
        Assert.Equal("user", webhookEvent.Source?.Type);
        Assert.Equal("U123", webhookEvent.Source?.UserId);
        Assert.Equal("text", webhookEvent.Message?.Type);
        Assert.Equal("hi", webhookEvent.Message?.Text);
    }

    [Fact]
    public async Task LineWebhookEndpoint_rejects_invalid_signature()
    {
        var context = CreateWebhookContext("""{"events":[]}""", "invalid-signature", CreateService());
        var called = false;

        await LineWebhookEndpoint.HandleAsync(
            context,
            (_, _, _, _) =>
            {
                called = true;
                return Task.CompletedTask;
            });

        Assert.False(called);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task LineWebhookEndpoint_dispatches_parsed_events()
    {
        var body = """
        {
          "events": [
            {
              "type": "message",
              "replyToken": "reply-token",
              "source": { "type": "user", "userId": "U123" },
              "message": { "id": "1", "type": "text", "text": "hello" }
            }
          ]
        }
        """;
        var service = CreateService();
        var context = CreateWebhookContext(body, CreateLineSignature(body, "secret"), service);
        LineWebhookEvent? capturedEvent = null;

        await LineWebhookEndpoint.HandleAsync(
            context,
            (webhookEvent, _, _, _) =>
            {
                capturedEvent = webhookEvent;
                return Task.CompletedTask;
            });

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("hello", capturedEvent?.Message?.Text);
    }

    [Fact]
    public async Task LineBotFactory_returns_named_bot_with_matching_token()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{}""", Encoding.UTF8, "application/json")
            };
        });
        var factory = new LineBotFactory(
            new StubHttpClientFactory(handler),
            Options.Create(new LineBotOptions
            {
                Bots =
                {
                    ["customer"] = new LineSettings
                    {
                        ChannelAccessToken = "customer-token",
                        ChannelSecret = "customer-secret"
                    },
                    ["admin"] = new LineSettings
                    {
                        ChannelAccessToken = "admin-token",
                        ChannelSecret = "admin-secret"
                    }
                }
            }));

        var adminBot = factory.GetBot("admin");
        await adminBot.PushTextMessageAsync("U123", "admin message");

        Assert.Equal("admin-token", capturedRequest?.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Named_LineWebhookEndpoint_uses_named_bot_for_signature_and_reply()
    {
        HttpRequestMessage? capturedReplyRequest = null;
        var httpHandler = new StubHttpMessageHandler(request =>
        {
            capturedReplyRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{}""", Encoding.UTF8, "application/json")
            };
        });
        var factory = new LineBotFactory(
            new StubHttpClientFactory(httpHandler),
            Options.Create(new LineBotOptions
            {
                Bots =
                {
                    ["customer"] = new LineSettings
                    {
                        ChannelAccessToken = "customer-token",
                        ChannelSecret = "customer-secret"
                    },
                    ["admin"] = new LineSettings
                    {
                        ChannelAccessToken = "admin-token",
                        ChannelSecret = "admin-secret"
                    }
                }
            }));
        var body = """
        {
          "events": [
            {
              "type": "message",
              "replyToken": "reply-token",
              "source": { "type": "user", "userId": "U123" },
              "message": { "id": "1", "type": "text", "text": "hello admin" }
            }
          ]
        }
        """;
        var context = CreateWebhookContext(body, CreateLineSignature(body, "admin-secret"), factory);

        await LineWebhookEndpoint.HandleAsync(
            context,
            "admin",
            async (webhookEvent, line, _, cancellationToken) =>
            {
                await line.ReplyTextMessageAsync(webhookEvent.ReplyToken!, "admin reply", cancellationToken);
            });

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("admin-token", capturedReplyRequest?.Headers.Authorization?.Parameter);
    }

    private static LineService CreateService(
        LineSettings? settings = null,
        HttpMessageHandler? handler = null)
    {
        var httpClient = new HttpClient(handler ?? new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
        {
            BaseAddress = new Uri("https://api.line.me")
        };

        return new LineService(
            httpClient,
            Options.Create(settings ?? new LineSettings
            {
                ChannelAccessToken = "token",
                ChannelSecret = "secret"
            }));
    }

    private static string CreateLineSignature(string body, string channelSecret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(channelSecret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }

    private static DefaultHttpContext CreateWebhookContext(
        string body,
        string signature,
        ILineService service)
    {
        var services = new ServiceCollection()
            .AddSingleton(service)
            .BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.Headers["x-line-signature"] = signature;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static DefaultHttpContext CreateWebhookContext(
        string body,
        string signature,
        ILineBotFactory factory)
    {
        var services = new ServiceCollection()
            .AddSingleton(factory)
            .BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.Headers["x-line-signature"] = signature;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _send;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send)
        {
            _send = send;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_send(request));
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler)
            {
                BaseAddress = new Uri("https://api.line.me")
            };
        }
    }
}
