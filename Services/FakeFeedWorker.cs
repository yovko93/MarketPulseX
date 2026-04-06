using MarketPulseX.Hubs;
using MarketPulseX.Models;
using Microsoft.AspNetCore.SignalR;

namespace MarketPulseX.Services
{
    public sealed class FakeFeedWorker : BackgroundService
    {
        private readonly MarketState _state;
        private readonly IHubContext<MarketHub> _hubContext;
        private readonly ILogger<FakeFeedWorker> _logger;

        public FakeFeedWorker(
            MarketState state,
            IHubContext<MarketHub> hubContext,
            ILogger<FakeFeedWorker> logger)
        {
            _state = state;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var random = new Random();
            decimal price = 21000m;

            _logger.LogInformation("FakeFeedWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                price += (decimal)((random.NextDouble() - 0.5) * 2.0);

                var tick = new MarketTick(
                    Symbol: "NQ",
                    Price: Math.Round(price, 2),
                    Size: random.Next(1, 10),
                    UtcTime: DateTime.UtcNow);

                _state.Add(tick);

                await _hubContext.Clients.All.SendAsync("tick", tick, stoppingToken);

                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
