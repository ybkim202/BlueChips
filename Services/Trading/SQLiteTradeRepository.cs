using BlueChips.Contracts;
using BlueChips.Models.Trading;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Services.Trading
{
    public sealed class SQLiteTradeRepository : ITradeRepository {
        private readonly string _cs;

        public SQLiteTradeRepository(Services.SettingsProvider settings) {
            var db = settings.Get().Database!.Path;
            _cs = new SqliteConnectionStringBuilder { DataSource = db, Cache = SqliteCacheMode.Shared }.ToString();
        }

        private static string Iso(DateTimeOffset dt) => dt.UtcDateTime.ToString("O");

        public async Task RunInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) {
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var tx = conn.BeginTransaction();
            try { await action(ct); await tx.CommitAsync(ct); }
            catch { await tx.RollbackAsync(ct); throw; }
        }

        public async Task SaveOrderAsync(Order o, CancellationToken ct = default) {
            const string SQL = @"
INSERT INTO Orders (id, ts, market, side, type, tif, price, stop_price, quantity, filled_qty, status, note)
VALUES (@id, @ts, @market, @side, @type, @tif, @price, @stop_price, @quantity, 0, @status, @note);";

            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL;
            BindOrder(cmd, o, includeFilled: false);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task UpdateOrderAsync(Order o, CancellationToken ct = default) {
            const string SQL = @"
UPDATE Orders SET
 ts=@ts, market=@market, side=@side, type=@type, tif=@tif, price=@price, stop_price=@stop_price,
 quantity=@quantity, filled_qty=@filled_qty, status=@status, note=@note
WHERE id=@id;";

            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL;
            BindOrder(cmd, o, includeFilled: true);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<Order?> GetOrderAsync(Guid id, CancellationToken ct = default) {
            const string SQL = "SELECT id, ts, market, side, type, tif, price, stop_price, quantity, filled_qty, status, note FROM Orders WHERE id=@id;";
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL;
            cmd.Parameters.AddWithValue("@id", id.ToString("N"));
            using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return null;
            return ReadOrder(r);
        }

        public async Task<IReadOnlyList<Order>> QueryOrdersAsync(string market, DateTimeOffset? from = null, DateTimeOffset? to = null, OrderStatus? status = null, CancellationToken ct = default) {
            var sql = "SELECT id, ts, market, side, type, tif, price, stop_price, quantity, filled_qty, status, note FROM Orders WHERE market=@market";
            if (from.HasValue) sql += " AND ts>=@from";
            if (to.HasValue) sql += " AND ts<=@to";
            if (status.HasValue) sql += " AND status=@status";
            sql += " ORDER BY ts ASC";

            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@market", market);
            if (from.HasValue) cmd.Parameters.AddWithValue("@from", Iso(from.Value));
            if (to.HasValue) cmd.Parameters.AddWithValue("@to", Iso(to.Value));
            if (status.HasValue) cmd.Parameters.AddWithValue("@status", status.Value.ToString());

            var list = new List<Order>();
            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) list.Add(ReadOrder(r));
            return list;
        }

        public async Task SaveFillAsync(Fill f, CancellationToken ct = default) {
            const string SQL = @"
INSERT INTO Fills (id, order_id, ts, market, side, price, quantity, fee)
VALUES (@id, @order_id, @ts, @market, @side, @price, @quantity, @fee)
ON CONFLICT(id) DO NOTHING;";
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL;

            cmd.Parameters.AddWithValue("@id", f.Id);
            cmd.Parameters.AddWithValue("@order_id", f.OrderId.ToString("N"));
            cmd.Parameters.AddWithValue("@ts", Iso(f.Ts));
            cmd.Parameters.AddWithValue("@market", f.Market);
            cmd.Parameters.AddWithValue("@side", f.Side.ToString());
            cmd.Parameters.AddWithValue("@price", f.Price);
            cmd.Parameters.AddWithValue("@quantity", f.Quantity);
            cmd.Parameters.AddWithValue("@fee", f.Fee);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<IReadOnlyList<Fill>> QueryFillsAsync(string market, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default) {
            var sql = "SELECT id, order_id, ts, market, side, price, quantity, fee FROM Fills WHERE market=@market";
            if (from.HasValue) sql += " AND ts>=@from";
            if (to.HasValue) sql += " AND ts<=@to";
            sql += " ORDER BY ts ASC";

            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@market", market);
            if (from.HasValue) cmd.Parameters.AddWithValue("@from", Iso(from.Value));
            if (to.HasValue) cmd.Parameters.AddWithValue("@to", Iso(to.Value));

            var list = new List<Fill>();
            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) {
                list.Add(new Fill
                {
                    Id = r.GetString(0),
                    OrderId = Guid.Parse(r.GetString(1)),
                    Ts = DateTimeOffset.Parse(r.GetString(2)),
                    Market = r.GetString(3),
                    Side = Enum.Parse<OrderSide>(r.GetString(4)),
                    Price = r.GetDecimal(5),
                    Quantity = r.GetDecimal(6),
                    Fee = r.GetDecimal(7)
                });
            }
            return list;
        }

        public async Task UpsertHoldingAsync(Holding h, CancellationToken ct = default) {
            const string SQL = @"
INSERT INTO Holdings (asset, free, locked) VALUES (@asset, @free, @locked)
ON CONFLICT(asset) DO UPDATE SET free=excluded.free, locked=excluded.locked;";
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL;
            cmd.Parameters.AddWithValue("@asset", h.Asset);
            cmd.Parameters.AddWithValue("@free", h.Free);
            cmd.Parameters.AddWithValue("@locked", h.Locked);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<IReadOnlyList<Holding>> GetHoldingsAsync(CancellationToken ct = default) {
            const string SQL = "SELECT asset, free, locked FROM Holdings;";
            using var conn = new SqliteConnection(_cs);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL;
            var list = new List<Holding>();
            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) {
                list.Add(new Holding { Asset = r.GetString(0), Free = r.GetDecimal(1), Locked = r.GetDecimal(2) });
            }
            return list;
        }

        private static void BindOrder(SqliteCommand cmd, Order o, bool includeFilled) {
            cmd.Parameters.AddWithValue("@id", o.Id.ToString("N"));
            cmd.Parameters.AddWithValue("@ts", Iso(o.Ts));
            cmd.Parameters.AddWithValue("@market", o.Market);
            cmd.Parameters.AddWithValue("@side", o.Side.ToString());
            cmd.Parameters.AddWithValue("@type", o.Type.ToString());
            cmd.Parameters.AddWithValue("@tif", o.TimeInForce.ToString());
            cmd.Parameters.AddWithValue("@price", (object?)o.Price ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@stop_price", (object?)o.StopPrice ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@quantity", o.Quantity);
            cmd.Parameters.AddWithValue("@status", o.Status.ToString());
            cmd.Parameters.AddWithValue("@note", (object?)o.Note ?? DBNull.Value);
            if (includeFilled) cmd.Parameters.AddWithValue("@filled_qty", o.FilledQty);
        }

        private static Order ReadOrder(SqliteDataReader r) {
            return new Order
            {
                Id = Guid.Parse(r.GetString(0)),
                Ts = DateTimeOffset.Parse(r.GetString(1)),
                Market = r.GetString(2),
                Side = Enum.Parse<OrderSide>(r.GetString(3)),
                Type = Enum.Parse<OrderType>(r.GetString(4)),
                TimeInForce = Enum.Parse<TimeInForce>(r.GetString(5)),
                Price = r.IsDBNull(6) ? null : r.GetDecimal(6),
                StopPrice = r.IsDBNull(7) ? null : r.GetDecimal(7),
                Quantity = r.GetDecimal(8),
                FilledQty = r.GetDecimal(9),
                Status = Enum.Parse<OrderStatus>(r.GetString(10)),
                Note = r.IsDBNull(11) ? null : r.GetString(11)
            };
        }
    }
}
