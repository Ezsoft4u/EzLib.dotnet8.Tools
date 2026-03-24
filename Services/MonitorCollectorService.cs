using EzLib.Models;
using Serilog;
using System.Text.Json;

namespace EzLib.Services
{
    public class MonitorCollectorService
    {
        private readonly MonitorCollectorSettings _settings;
        private readonly IMonitorStorageService _storage;
        private readonly IHttpClientFactory _httpClientFactory;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MonitorCollectorService(
            MonitorCollectorSettings settings,
            IMonitorStorageService storage,
            IHttpClientFactory httpClientFactory)
        {
            _settings = settings;
            _storage = storage;
            _httpClientFactory = httpClientFactory;
        }

        public async Task CollectAllAsync(CancellationToken ct = default)
        {
            var tasks = _settings.Hosts.Select(h => CollectOneAsync(h, ct));
            await Task.WhenAll(tasks);
        }

        private async Task CollectOneAsync(MonitorHostSettings host, CancellationToken ct)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(host.TimeoutSeconds);
                if (!string.IsNullOrEmpty(host.ApiKey))
                    client.DefaultRequestHeaders.Add("X-Api-Key", host.ApiKey);

                var response = await client.GetAsync(host.Url, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<SystemMonitorResult>(json, _jsonOpts);
                if (result == null)
                {
                    Log.Warning("[MonitorCollector] {Host}: 回傳 JSON 無法解析", host.Name);
                    return;
                }

                var entity = MapToEntity(host.Name, result);
                await _storage.SaveSnapshotAsync(entity);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[MonitorCollector] 收集主機 {Host} 資料失敗", host.Name);
            }
        }

        private static MonitorSnapshotEntity MapToEntity(string hostName, SystemMonitorResult r)
        {
            return new MonitorSnapshotEntity
            {
                HostName = hostName,
                CollectedAt = r.CollectedAt == default ? DateTime.UtcNow : r.CollectedAt,
                OverallStatus = r.OverallStatus,
                CpuUsagePercent = r.Cpu?.UsagePercent ?? 0,
                WorkingSetMB = r.Process?.WorkingSetMB ?? 0,
                PrivateMemoryMB = r.Process?.PrivateMemoryMB ?? 0,
                ThreadCount = r.Process?.ThreadCount ?? 0,
                UptimeSeconds = r.Process?.UptimeSeconds ?? 0,
                SystemRamTotalMB = r.SystemRam?.TotalMB ?? 0,
                SystemRamUsedMB = r.SystemRam?.UsedMB ?? 0,
                SystemRamUsedPercent = r.SystemRam?.UsedPercent ?? 0,
                GcTotalHeapMB = r.Gc?.TotalHeapMB ?? 0,
                DisksJson = r.Disks?.Count > 0
                    ? JsonSerializer.Serialize(r.Disks) : null,
                DatabasesJson = r.Databases?.Count > 0
                    ? JsonSerializer.Serialize(r.Databases) : null,
                HttpJson = r.Http?.Count > 0
                    ? JsonSerializer.Serialize(r.Http) : null,
            };
        }
    }
}
