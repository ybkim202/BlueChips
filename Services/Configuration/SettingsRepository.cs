using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlueChips.Contracts;
using Microsoft.Data.Sqlite;

namespace BlueChips.Services.Configuration {
    public sealed class SettingsRepository : ISettingsRepository {
        private readonly string _cs;

        public SettingsRepository(Services.SettingsProvider settings) {
            var db = settings.Get().Database!.Path;
            _cs = new SqliteConnectionStringBuilder { DataSource = db, Cache = SqliteCacheMode.Shared }.ToString();
            EnsureTable().GetAwaiter().GetResult();
        }

        private async Task EnsureTable() {
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS AppSettings(key TEXT PRIMARY KEY, value TEXT NOT NULL);";
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SetAsync<T>(string key, T value, CancellationToken ct = default) {
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO AppSettings(key, value) VALUES(@k, @v) ON CONFLICT(key) DO UPDATE SET value=excluded.value;";
            cmd.Parameters.AddWithValue("@k", key);
            cmd.Parameters.AddWithValue("@v", value?.ToString() ?? "");
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) {
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT value FROM AppSettings WHERE key=@k;";
            cmd.Parameters.AddWithValue("@k", key);
            var obj = await cmd.ExecuteScalarAsync(ct);
            if (obj is null) return default;
            var s = obj.ToString() ?? "";
            return (T?)System.Convert.ChangeType(s, typeof(T));
        }

        public async Task RemoveAsync(string key, CancellationToken ct = default) {
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM AppSettings WHERE key=@k;";
            cmd.Parameters.AddWithValue("@k", key);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default) {
            var dict = new Dictionary<string, string>();
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT key, value FROM AppSettings;";
            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) dict[r.GetString(0)] = r.GetString(1);
            return dict;
        }
    }
}
