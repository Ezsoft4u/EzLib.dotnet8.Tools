using EzLib.Models;

namespace EzLib.Services
{
    public interface IMonitorStorageService
    {
        Task EnsureTableAsync();
        Task SaveSnapshotAsync(MonitorSnapshotEntity entity);
        Task<List<MonitorSnapshotEntity>> GetSnapshotsAsync(string? hostName, DateTime from, DateTime to);
        Task<List<string>> GetHostNamesAsync();
        Task DeleteOldSnapshotsAsync(int retentionDays);
    }
}
