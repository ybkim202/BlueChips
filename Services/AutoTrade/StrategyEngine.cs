using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlueChips.Contracts;
using BlueChips.Models.AutoTrade;

namespace BlueChips.Services.AutoTrade {
    public sealed class StrategyEngine : IStrategyEngine {
        private readonly ConcurrentDictionary<string, Strategy> _store = new();
        private readonly ConcurrentDictionary<string, string> _state = new();

        public event Action<string, string>? StatusChanged;

        public Task<string> SaveAsync(Strategy strategy, CancellationToken ct = default) {
            var id = string.IsNullOrWhiteSpace(strategy.Id) ? Guid.NewGuid().ToString("N") : strategy.Id;
            strategy.Id = id;
            _store[id] = strategy;
            return Task.FromResult(id);
        }

        public Task<Strategy?> GetAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(id, out var s) ? s : null);

        public Task<IReadOnlyList<Strategy>> ListAsync(CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Strategy>)new List<Strategy>(_store.Values));

        public Task<bool> StartAsync(string id, CancellationToken ct = default) {
            if (!_store.ContainsKey(id)) return Task.FromResult(false);
            _state[id] = "Running";
            StatusChanged?.Invoke(id, "Running");
            return Task.FromResult(true);
        }

        public Task<bool> StopAsync(string id, CancellationToken ct = default) {
            if (!_store.ContainsKey(id)) return Task.FromResult(false);
            _state[id] = "Stopped";
            StatusChanged?.Invoke(id, "Stopped");
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_store.TryRemove(id, out _));
    }
}
