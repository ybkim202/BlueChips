using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueChips.Contracts;
using BlueChips.Models.Trading;

namespace BlueChips.Services.Trading {
    /// <summary>
    /// SQLite 대체용 메모리 리포지토리 (테스트/MVP/백테스트용).
    /// - 스레드 안전(Read/Write Lock)
    /// - 입출력 모두 딥 카피로 외부 변조 차단
    /// - 트랜잭션은 전역 락으로 에뮬레이션
    /// </summary>
    public sealed class InMemoryTradeRepository : ITradeRepository {
        private readonly Dictionary<Guid, Order> _orders = new();
        private readonly List<Fill> _fills = new();
        private readonly Dictionary<string, Holding> _holdings = new(); // key: Asset

        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        public Task RunInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) {
            _lock.EnterWriteLock();
            try { return action(ct); }
            finally { _lock.ExitWriteLock(); }
        }

        // ---------------- Orders ----------------

        public Task SaveOrderAsync(Order o, CancellationToken ct = default) {
            _lock.EnterWriteLock();
            try {
                // upsert 성격(동일 키면 덮어씀). 필요 시 존재 여부 검사해 예외로 바꿔도 됨.
                _orders[o.Id] = Clone(o);
                return Task.CompletedTask;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public Task UpdateOrderAsync(Order o, CancellationToken ct = default) {
            _lock.EnterWriteLock();
            try {
                if (_orders.ContainsKey(o.Id))
                    _orders[o.Id] = Clone(o);
                else
                    _orders[o.Id] = Clone(o); // 없는 경우에도 저장(upsert)
                return Task.CompletedTask;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public Task<Order?> GetOrderAsync(Guid id, CancellationToken ct = default) {
            _lock.EnterReadLock();
            try {
                return Task.FromResult(_orders.TryGetValue(id, out var o) ? Clone(o) : null);
            }
            finally { _lock.ExitReadLock(); }
        }

        public Task<IReadOnlyList<Order>> QueryOrdersAsync(
            string market, DateTimeOffset? from = null, DateTimeOffset? to = null,
            OrderStatus? status = null, CancellationToken ct = default) {
            _lock.EnterReadLock();
            try {
                IEnumerable<Order> q = _orders.Values.Where(x => x.Market == market);
                if (from.HasValue) q = q.Where(x => x.Ts >= from.Value);
                if (to.HasValue) q = q.Where(x => x.Ts <= to.Value);
                if (status.HasValue) q = q.Where(x => x.Status == status.Value);

                var list = q.OrderBy(x => x.Ts).Select(Clone).ToList();
                return Task.FromResult<IReadOnlyList<Order>>(list);
            }
            finally { _lock.ExitReadLock(); }
        }

        // ---------------- Fills ----------------

        public Task SaveFillAsync(Fill f, CancellationToken ct = default) {
            _lock.EnterWriteLock();
            try {
                // 동일 Id 중복 방지
                if (_fills.Any(x => x.Id == f.Id)) return Task.CompletedTask;
                _fills.Add(Clone(f));
                return Task.CompletedTask;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public Task<IReadOnlyList<Fill>> QueryFillsAsync(
            string market, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default) {
            _lock.EnterReadLock();
            try {
                IEnumerable<Fill> q = _fills.Where(x => x.Market == market);
                if (from.HasValue) q = q.Where(x => x.Ts >= from.Value);
                if (to.HasValue) q = q.Where(x => x.Ts <= to.Value);

                var list = q.OrderBy(x => x.Ts).Select(Clone).ToList();
                return Task.FromResult<IReadOnlyList<Fill>>(list);
            }
            finally { _lock.ExitReadLock(); }
        }

        // ---------------- Holdings ----------------

        public Task UpsertHoldingAsync(Holding h, CancellationToken ct = default) {
            _lock.EnterWriteLock();
            try {
                _holdings[h.Asset] = Clone(h);
                return Task.CompletedTask;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public Task<IReadOnlyList<Holding>> GetHoldingsAsync(CancellationToken ct = default) {
            _lock.EnterReadLock();
            try {
                var list = _holdings.Values.Select(Clone).ToList();
                return Task.FromResult<IReadOnlyList<Holding>>(list);
            }
            finally { _lock.ExitReadLock(); }
        }

        // ---------------- Clone helpers ----------------

        private static Order Clone(Order o) => new()
        {
            Id = o.Id,
            Ts = o.Ts,
            Market = o.Market,
            Side = o.Side,
            Type = o.Type,
            TimeInForce = o.TimeInForce,
            Price = o.Price,
            StopPrice = o.StopPrice,
            Quantity = o.Quantity,
            FilledQty = o.FilledQty,
            Status = o.Status,
            Note = o.Note
        };

        private static Fill Clone(Fill f) => new()
        {
            Id = f.Id,
            OrderId = f.OrderId,
            Market = f.Market,
            Side = f.Side,
            Ts = f.Ts,
            Price = f.Price,
            Quantity = f.Quantity,
            Fee = f.Fee
        };

        private static Holding Clone(Holding h) => new()
        {
            Asset = h.Asset,
            Free = h.Free,
            Locked = h.Locked
        };
    }
}
