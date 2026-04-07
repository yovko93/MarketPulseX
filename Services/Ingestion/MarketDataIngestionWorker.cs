using MarketPulseX.Hubs;
using MarketPulseX.Services.Ingestion.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace MarketPulseX.Services.Ingestion;

public sealed class MarketDataIngestionWorker : BackgroundService
{
    private readonly MarketState _marketState;
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly ILogger<MarketDataIngestionWorker> _logger;
    private readonly IMarketDataAdapter _adapter;

    public MarketDataIngestionWorker(
        MarketState marketState,
        IHubContext<MarketHub> hubContext,
        ILogger<MarketDataIngestionWorker> logger,
        IOptions<IngestionOptions> options,
        IMarketDataAdapterSelector selector)
    {
        _marketState = marketState;
        _hubContext = hubContext;
        _logger = logger;

        _adapter = selector.Select(options.Value.Adapter);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ingestion worker started with adapter: {Adapter}", _adapter.FeedName);

        await foreach (var tick in _adapter.StreamAsync(stoppingToken))
        {
            _marketState.Add(tick);
            await _hubContext.Clients.All.SendAsync("tick", tick, stoppingToken);
        }
    }
}
