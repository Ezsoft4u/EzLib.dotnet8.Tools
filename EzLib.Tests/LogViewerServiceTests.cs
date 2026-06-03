using EzLib.Models;
using EzLib.Middleware;
using EzLib.Services;
using EzLib.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Xunit;

namespace EzLib.Tests;

public class LogViewerServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public LogViewerServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ezlib-log-viewer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task ListLogFilesAsync_returns_files_from_configured_log_directory()
    {
        var olderPath = Path.Combine(_tempDirectory, "log-20260601.txt");
        var newerPath = Path.Combine(_tempDirectory, "log-20260602.txt");
        await File.WriteAllTextAsync(olderPath, "older");
        await File.WriteAllTextAsync(newerPath, "newer");
        File.SetLastWriteTimeUtc(olderPath, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(newerPath, new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc));
        var service = CreateService();

        var files = await service.ListLogFilesAsync();

        Assert.Collection(
            files,
            file => Assert.Equal("log-20260602.txt", file.Name),
            file => Assert.Equal("log-20260601.txt", file.Name));
    }

    [Fact]
    public async Task ReadLogFileAsync_filters_by_date_range_and_keeps_continuation_lines()
    {
        const string logContent = """
        2026-06-02 09:00:00.000 +08:00 [INF] before range
        2026-06-02 10:00:00.000 +08:00 [ERR] failed to save order
        System.InvalidOperationException: database timeout
           at Demo.Service.Save()
        2026-06-02 11:00:00.000 +08:00 [INF] after range
        """;
        await File.WriteAllTextAsync(Path.Combine(_tempDirectory, "log-20260602.txt"), logContent);
        var service = CreateService();

        var result = await service.ReadLogFileAsync(new LogReadRequest
        {
            FileName = "log-20260602.txt",
            From = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.FromHours(8)),
            To = new DateTimeOffset(2026, 6, 2, 10, 30, 0, TimeSpan.FromHours(8))
        });

        Assert.Contains("failed to save order", result.Content);
        Assert.Contains("System.InvalidOperationException", result.Content);
        Assert.DoesNotContain("before range", result.Content);
        Assert.DoesNotContain("after range", result.Content);
    }

    [Fact]
    public async Task ReadLogFileAsync_rejects_path_traversal_file_names()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReadLogFileAsync(new LogReadRequest
            {
                FileName = "..\\secret.txt"
            }));
    }

    [Fact]
    public async Task LogViewerMiddleware_returns_file_list_api()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDirectory, "log-20260603.txt"), "today");
        var settings = CreateSettings();
        var service = new FileLogViewerService(settings);
        var nextCalled = false;
        var middleware = new LogViewerMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            settings,
            service);
        var context = new DefaultHttpContext();
        context.Request.Path = "/logs/api/files";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("log-20260603.txt", body);
    }

    [Fact]
    public async Task LogViewerMiddleware_html_declares_inline_favicon()
    {
        var settings = CreateSettings();
        var service = new FileLogViewerService(settings);
        var middleware = new LogViewerMiddleware(_ => Task.CompletedTask, settings, service);
        var context = new DefaultHttpContext();
        context.Request.Path = "/logs";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        Assert.Contains("""<link rel="icon" href="data:,">""", body);
    }

    [Fact]
    public async Task LogViewerMiddleware_ignores_paths_that_only_share_prefix()
    {
        var settings = CreateSettings();
        settings.RequireViewerKey = true;
        settings.ViewerKey = "secret";
        var service = new FileLogViewerService(settings);
        var nextCalled = false;
        var middleware = new LogViewerMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            settings,
            service);
        var context = new DefaultHttpContext();
        context.Request.Path = "/logs2/api/files";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task AddLogViewer_reads_log_directory_from_appsettings_key()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDirectory, "system-20260603.txt"), "from appsettings key");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemLogDirectory"] = _tempDirectory
            })
            .Build();
        var provider = new ServiceCollection()
            .AddLogViewer(configuration, "SystemLogDirectory")
            .BuildServiceProvider();

        var service = provider.GetRequiredService<ILogViewerService>();
        var files = await service.ListLogFilesAsync();

        Assert.Contains(files, file => file.Name == "system-20260603.txt");
    }

    [Fact]
    public async Task AddLogViewer_accepts_log_directory_directly()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDirectory, "direct-20260603.txt"), "from startup value");
        var provider = new ServiceCollection()
            .AddLogViewer(_tempDirectory)
            .BuildServiceProvider();

        var service = provider.GetRequiredService<ILogViewerService>();
        var files = await service.ListLogFilesAsync();

        Assert.Contains(files, file => file.Name == "direct-20260603.txt");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    private FileLogViewerService CreateService()
    {
        return new FileLogViewerService(CreateSettings());
    }

    private LogViewerSettings CreateSettings()
    {
        return new LogViewerSettings
        {
            LogDirectory = _tempDirectory,
            FileSearchPattern = "*.txt"
        };
    }
}
