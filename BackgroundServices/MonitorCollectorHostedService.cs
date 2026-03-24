using EzLib.Models;
using EzLib.Services;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace EzLib.BackgroundServices
{
    public class MonitorCollectorHostedService : IHostedService, IAsyncDisposable
    {
        private readonly MonitorCollectorSettings _settings;
        private readonly IMonitorStorageService _storage;
        private readonly MonitorCollectorService _collector;
        private PeriodicTimer? _collectTimer;
        private PeriodicTimer? _cleanupTimer;
        private Task? _collectLoop;
        private Task? _cleanupLoop;
        private readonly CancellationTokenSource _cts = new();

        public MonitorCollectorHostedService(
            MonitorCollectorSettings settings,
            IMonitorStorageService storage,
            MonitorCollectorService collector)
        {
            _settings = settings;
            _storage = storage;
            _collector = collector;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Information("[MonitorCollector] 啟動，間隔 {Seconds}s，主機數 {Count}",
                _settings.CollectIntervalSeconds, _settings.Hosts.Count);

            try
            {
                await _storage.EnsureTableAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[MonitorCollector] EnsureTableAsync 失敗，請確認 StorageConnectionString");
            }

            _collectTimer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.CollectIntervalSeconds));
            _collectLoop = RunCollectLoopAsync(_cts.Token);

            if (_settings.RetentionDays > 0)
            {
                _cleanupTimer = new PeriodicTimer(TimeSpan.FromHours(24));
                _cleanupLoop = RunCleanupLoopAsync(_cts.Token);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            if (_collectLoop != null) await _collectLoop.ConfigureAwait(false);
            if (_cleanupLoop != null) await _cleanupLoop.ConfigureAwait(false);
            Log.Information("[MonitorCollector] 已停止");
        }

        private async Task RunCollectLoopAsync(CancellationToken ct)
        {
            try
            {
                while (await _collectTimer!.WaitForNextTickAsync(ct))
                {
                    await _collector.CollectAllAsync(ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log.Error(ex, "[MonitorCollector] 收集迴圈意外終止");
            }
        }

        private async Task RunCleanupLoopAsync(CancellationToken ct)
        {
            try
            {
                while (await _cleanupTimer!.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        await _storage.DeleteOldSnapshotsAsync(_settings.RetentionDays);
                        Log.Debug("[MonitorCollector] 已清除超過 {Days} 天的資料", _settings.RetentionDays);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "[MonitorCollector] 清除舊資料失敗");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _collectTimer?.Dispose();
            _cleanupTimer?.Dispose();
            _cts.Dispose();
            await ValueTask.CompletedTask;
        }
    }
}
