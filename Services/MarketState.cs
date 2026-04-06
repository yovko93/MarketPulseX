using MarketPulseX.Models;
using System.Collections.Concurrent;

namespace MarketPulseX.Services
{
    public sealed class MarketState
    {
        private readonly ConcurrentQueue<MarketTick> _ticks = new();
        private const int MaxTicks = 10_000;

        private long _totalTicks;
        private DateTime? _lastTickUtc;

        public void Add(MarketTick tick)
        {
            _ticks.Enqueue(tick);
            Interlocked.Increment(ref _totalTicks);
            _lastTickUtc = tick.UtcTime;

            while (_ticks.Count > MaxTicks)
                _ticks.TryDequeue(out _);
        }

        public IReadOnlyList<MarketTick> GetLatest(int take = 100)
            => _ticks.Reverse().Take(take).ToList();

        public object GetStatus() => new
        {
            TotalTicks = Interlocked.Read(ref _totalTicks),
            BufferedTicks = _ticks.Count,
            LastTickUtc = _lastTickUtc
        };
    }
}
