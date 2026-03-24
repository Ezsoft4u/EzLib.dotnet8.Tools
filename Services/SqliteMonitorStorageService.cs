using EzLib.Models;
using Microsoft.Data.Sqlite;

namespace EzLib.Services
{
    /// <summary>
    /// 以 SQLite 本地檔案作為儲存後端的 <see cref="IMonitorStorageService"/> 實作。
    /// 不需要任何資料庫伺服器，適合開發環境或單機部署。
    /// </summary>
    public class SqliteMonitorStorageService : IMonitorStorageService
    {
        private readonly string _connectionString;

        /// <param name="filePathOrConnectionString">
        /// SQLite 檔案路徑（例如 "monitor.db"）或完整 connection string（例如 "Data Source=monitor.db"）
        /// </param>
        public SqliteMonitorStorageService(string filePathOrConnectionString)
        {
            // 若傳入的是純檔案路徑，自動補上 "Data Source="
            _connectionString = filePathOrConnectionString.TrimStart().StartsWith("Data Source", StringComparison.OrdinalIgnoreCase)
                ? filePathOrConnectionString
                : $"Data Source={filePathOrConnectionString}";
        }

        public async Task EnsureTableAsync()
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS MonitorSnapshots (
    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    HostName            TEXT    NOT NULL,
    CollectedAt         TEXT    NOT NULL,
    OverallStatus       TEXT    NOT NULL,
    CpuUsagePercent     REAL    NOT NULL DEFAULT 0,
    WorkingSetMB        REAL    NOT NULL DEFAULT 0,
    PrivateMemoryMB     REAL    NOT NULL DEFAULT 0,
    ThreadCount         INTEGER NOT NULL DEFAULT 0,
    UptimeSeconds       REAL    NOT NULL DEFAULT 0,
    SystemRamTotalMB    REAL    NOT NULL DEFAULT 0,
    SystemRamUsedMB     REAL    NOT NULL DEFAULT 0,
    SystemRamUsedPercent REAL   NOT NULL DEFAULT 0,
    GcTotalHeapMB       REAL    NOT NULL DEFAULT 0,
    DisksJson           TEXT    NULL,
    DatabasesJson       TEXT    NULL,
    HttpJson            TEXT    NULL
);
CREATE INDEX IF NOT EXISTS IX_MonitorSnapshots_Host_Time
    ON MonitorSnapshots(HostName, CollectedAt DESC);";

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand(sql, conn);
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

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@HostName",             entity.HostName);
            cmd.Parameters.AddWithValue("@CollectedAt",          entity.CollectedAt.ToString("o")); // ISO 8601
            cmd.Parameters.AddWithValue("@OverallStatus",        entity.OverallStatus);
            cmd.Parameters.AddWithValue("@CpuUsagePercent",      entity.CpuUsagePercent);
            cmd.Parameters.AddWithValue("@WorkingSetMB",         entity.WorkingSetMB);
            cmd.Parameters.AddWithValue("@PrivateMemoryMB",      entity.PrivateMemoryMB);
            cmd.Parameters.AddWithValue("@ThreadCount",          entity.ThreadCount);
            cmd.Parameters.AddWithValue("@UptimeSeconds",        entity.UptimeSeconds);
            cmd.Parameters.AddWithValue("@SystemRamTotalMB",     entity.SystemRamTotalMB);
            cmd.Parameters.AddWithValue("@SystemRamUsedMB",      entity.SystemRamUsedMB);
            cmd.Parameters.AddWithValue("@SystemRamUsedPercent", entity.SystemRamUsedPercent);
            cmd.Parameters.AddWithValue("@GcTotalHeapMB",        entity.GcTotalHeapMB);
            cmd.Parameters.AddWithValue("@DisksJson",     (object?)entity.DisksJson     ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DatabasesJson", (object?)entity.DatabasesJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HttpJson",      (object?)entity.HttpJson      ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<MonitorSnapshotEntity>> GetSnapshotsAsync(
            string? hostName, DateTime from, DateTime to)
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
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@From", from.ToString("o"));
            cmd.Parameters.AddWithValue("@To",   to.ToString("o"));
            if (!string.IsNullOrEmpty(hostName))
                cmd.Parameters.AddWithValue("@HostName", hostName);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new MonitorSnapshotEntity
                {
                    Id                   = reader.GetInt64(0),
                    HostName             = reader.GetString(1),
                    CollectedAt          = DateTime.Parse(reader.GetString(2),
                                              null, System.Globalization.DateTimeStyles.RoundtripKind),
                    OverallStatus        = reader.GetString(3),
                    CpuUsagePercent      = reader.GetDouble(4),
                    WorkingSetMB         = reader.GetDouble(5),
                    PrivateMemoryMB      = reader.GetDouble(6),
                    ThreadCount          = (int)reader.GetInt64(7),
                    UptimeSeconds        = reader.GetDouble(8),
                    SystemRamTotalMB     = reader.GetDouble(9),
                    SystemRamUsedMB      = reader.GetDouble(10),
                    SystemRamUsedPercent = reader.GetDouble(11),
                    GcTotalHeapMB        = reader.GetDouble(12),
                    DisksJson            = reader.IsDBNull(13) ? null : reader.GetString(13),
                    DatabasesJson        = reader.IsDBNull(14) ? null : reader.GetString(14),
                    HttpJson             = reader.IsDBNull(15) ? null : reader.GetString(15),
                });
            }
            return result;
        }

        public async Task<List<string>> GetHostNamesAsync()
        {
            const string sql = "SELECT DISTINCT HostName FROM MonitorSnapshots ORDER BY HostName";
            var result = new List<string>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(reader.GetString(0));
            return result;
        }

        public async Task DeleteOldSnapshotsAsync(int retentionDays)
        {
            if (retentionDays <= 0) return;
            const string sql = "DELETE FROM MonitorSnapshots WHERE CollectedAt < @Cutoff";
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Cutoff", DateTime.UtcNow.AddDays(-retentionDays).ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
