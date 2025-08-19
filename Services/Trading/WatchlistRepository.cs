using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlueChips.Contracts;
using BlueChips.Models.Trading;
using Microsoft.Data.Sqlite;

namespace BlueChips.Services.Trading {
    public sealed class WatchlistRepository : IWatchlistRepository {
        private readonly string _cs;
        public WatchlistRepository(Services.SettingsProvider settings) {
            var db = settings.Get().Database!.Path;
            _cs = new SqliteConnectionStringBuilder { DataSource = db, Cache = SqliteCacheMode.Shared }.ToString();
        }

        public async Task<IReadOnlyList<WatchlistEntry>> GetAllAsync(CancellationToken ct = default) {
            const string SQL = "SELECT symbol, sort, note FROM Watchlist ORDER BY sort ASC;";
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL;

            var list = new List<WatchlistEntry>();
            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                list.Add(new WatchlistEntry { Symbol = r.GetString(0), Sort = r.GetInt32(1), Note = r.IsDBNull(2) ? null : r.GetString(2) });
            return list;
        }

        public async Task AddAsync(WatchlistEntry entry, CancellationToken ct = default) {
            const string SQL = "INSERT OR REPLACE INTO Watchlist(symbol, sort, note) VALUES(@s, @i, @n);";
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL;
            cmd.Parameters.AddWithValue("@s", entry.Symbol);
            cmd.Parameters.AddWithValue("@i", entry.Sort);
            cmd.Parameters.AddWithValue("@n", (object?)entry.Note ?? System.DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task RemoveAsync(string symbol, CancellationToken ct = default) {
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Watchlist WHERE symbol=@s;";
            cmd.Parameters.AddWithValue("@s", symbol);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task ReorderAsync(IReadOnlyList<string> orderedSymbols, CancellationToken ct = default) {
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;

            for (int i = 0; i < orderedSymbols.Count; i++) {
                cmd.CommandText = "UPDATE Watchlist SET sort=@i WHERE symbol=@s;";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@i", i);
                cmd.Parameters.AddWithValue("@s", orderedSymbols[i]);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            await tx.CommitAsync(ct);
        }
    }
}
