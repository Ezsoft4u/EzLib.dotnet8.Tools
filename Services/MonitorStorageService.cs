using EzLib.Models;
using Microsoft.Data.SqlClient;

namespace EzLib.Services
{
    public class MonitorStorageService : IMonitorStorageService
    {
        private readonly string _connectionString;

        public MonitorStorageService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTableAsync()
        {
            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MonitorSnapshots')
BEGIN
    CREATE TABLE MonitorSnapshots (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        HostName NVARCHAR(100) NOT NULL,
        CollectedAt DATETIME2 NOT NULL,
        OverallStatus NVARCHAR(20) NOT NULL,
        CpuUsagePercent FLOAT NOT NULL DEFAULT 0,
        WorkingSetMB FLOAT NOT NULL DEFAULT 0,
        PrivateMemoryMB FLOAT NOT NULL DEFAULT 0,
        ThreadCount INT NOT NULL DEFAULT 0,
        UptimeSeconds FLOAT NOT NULL DEFAULT 0,
        SystemRamTotalMB FLOAT NOT NULL DEFAULT 0,
        SystemRamUsedMB FLOAT NOT NULL DEFAULT 0,
        SystemRamUsedPercent FLOAT NOT NULL DEFAULT 0,
        GcTotalHeapMB FLOAT NOT NULL DEFAULT 0,
        DisksJson NVARCHAR(MAX) NULL,
        DatabasesJson NVARCHAR(MAX) NULL,
        HttpJson NVARCHAR(MAX) NULL
    );
    CREATE INDEX IX_MonitorSnapshots_Host_Time ON MonitorSnapshots(HostName, CollectedAt DESC);
END";
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveSnapshotAsync(MonitorSnapshotEntity entity)
        {
            const string sql = @"
INSERT INTO MonitorSnapshots
    (HostName, CollectedAt, OverallStatus, CpuUsagePercent, WorkingSetMB, PrivateMemoryMB,
     ThreadCount, UptimeSeconds, SystemRamTotalMB, SystemRamUsedMB, SystemRamUsedPercent,
     GcTotalHeapMB, DisksJson, DatabasesJson, HttpJson)
VALUES
    (@HostName, @CollectedAt, @OverallStatus, @CpuUsagePercent, @WorkingSetMB, @PrivateMemoryMB,
     @ThreadCount, @UptimeSeconds, @SystemRamTotalMB, @SystemRamUsedMB, @SystemRamUsedPercent,
     @GcTotalHeapMB, @DisksJson, @DatabasesJson, @HttpJson)";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@HostName", entity.HostName);
            cmd.Parameters.AddWithValue("@CollectedAt", entity.CollectedAt);
            cmd.Parameters.AddWithValue("@OverallStatus", entity.OverallStatus);
            cmd.Parameters.AddWithValue("@CpuUsagePercent", entity.CpuUsagePercent);
            cmd.Parameters.AddWithValue("@WorkingSetMB", entity.WorkingSetMB);
            cmd.Parameters.AddWithValue("@PrivateMemoryMB", entity.PrivateMemoryMB);
            cmd.Parameters.AddWithValue("@ThreadCount", entity.ThreadCount);
            cmd.Parameters.AddWithValue("@UptimeSeconds", entity.UptimeSeconds);
            cmd.Parameters.AddWithValue("@SystemRamTotalMB", entity.SystemRamTotalMB);
            cmd.Parameters.AddWithValue("@SystemRamUsedMB", entity.SystemRamUsedMB);
            cmd.Parameters.AddWithValue("@SystemRamUsedPercent", entity.SystemRamUsedPercent);
            cmd.Parameters.AddWithValue("@GcTotalHeapMB", entity.GcTotalHeapMB);
            cmd.Parameters.AddWithValue("@DisksJson", (object?)entity.DisksJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DatabasesJson", (object?)entity.DatabasesJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HttpJson", (object?)entity.HttpJson ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<MonitorSnapshotEntity>> GetSnapshotsAsync(string? hostName, DateTime from, DateTime to)
        {
            var sql = @"
SELECT Id, HostName, CollectedAt, OverallStatus, CpuUsagePercent, WorkingSetMB, PrivateMemoryMB,
       ThreadCount, UptimeSeconds, SystemRamTotalMB, SystemRamUsedMB, SystemRamUsedPercent,
       GcTotalHeapMB, DisksJson, DatabasesJson, HttpJson
FROM MonitorSnapshots
WHERE CollectedAt >= @From AND CollectedAt <= @To";

            if (!string.IsNullOrEmpty(hostName))
                sql += " AND HostName = @HostName";

            sql += " ORDER BY CollectedAt ASC";

            var result = new List<MonitorSnapshotEntity>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@From", from);
            cmd.Parameters.AddWithValue("@To", to);
            if (!string.IsNullOrEmpty(hostName))
                cmd.Parameters.AddWithValue("@HostName", hostName);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new MonitorSnapshotEntity
                {
                    Id = reader.GetInt64(0),
                    HostName = reader.GetString(1),
                    CollectedAt = reader.GetDateTime(2),
                    OverallStatus = reader.GetString(3),
                    CpuUsagePercent = reader.GetDouble(4),
                    WorkingSetMB = reader.GetDouble(5),
                    PrivateMemoryMB = reader.GetDouble(6),
                    ThreadCount = reader.GetInt32(7),
                    UptimeSeconds = reader.GetDouble(8),
                    SystemRamTotalMB = reader.GetDouble(9),
                    SystemRamUsedMB = reader.GetDouble(10),
                    SystemRamUsedPercent = reader.GetDouble(11),
                    GcTotalHeapMB = reader.GetDouble(12),
                    DisksJson = reader.IsDBNull(13) ? null : reader.GetString(13),
                    DatabasesJson = reader.IsDBNull(14) ? null : reader.GetString(14),
                    HttpJson = reader.IsDBNull(15) ? null : reader.GetString(15),
                });
            }
            return result;
        }

        public async Task<List<string>> GetHostNamesAsync()
        {
            const string sql = "SELECT DISTINCT HostName FROM MonitorSnapshots ORDER BY HostName";
            var result = new List<string>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(reader.GetString(0));
            return result;
        }

        public async Task DeleteOldSnapshotsAsync(int retentionDays)
        {
            if (retentionDays <= 0) return;
            const string sql = "DELETE FROM MonitorSnapshots WHERE CollectedAt < @Cutoff";
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Cutoff", DateTime.UtcNow.AddDays(-retentionDays));
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
