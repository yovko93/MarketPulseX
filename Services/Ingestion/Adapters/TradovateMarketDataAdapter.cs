using MarketPulseX.Models;
using MarketPulseX.Services.Ingestion.Options;
using Microsoft.Extensions.Options;

namespace MarketPulseX.Services.Ingestion.Adapters;

public sealed class TradovateMarketDataAdapter : IMarketDataAdapter
{
    private readonly IngestionOptions _options;
    private readonly ILogger<TradovateMarketDataAdapter> _logger;

    public TradovateMarketDataAdapter(
        IOptions<IngestionOptions> options,
        ILogger<TradovateMarketDataAdapter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string FeedName => "Tradovate";

    public async IAsyncEnumerable<MarketTick> StreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Skeleton implementation: replace with real Tradovate WS/API handshake.
        var rng = new Random();
        decimal price = 21000m;

        _logger.LogInformation("{Adapter} adapter streaming symbol {Symbol}", FeedName, _options.Symbol);

        while (!cancellationToken.IsCancellationRequested)
        {
            price += (decimal)((rng.NextDouble() - 0.5) * 2.0);

            yield return new MarketTick(
                Symbol: _options.Symbol,
                Price: Math.Round(price, 2),
                Size: rng.Next(1, 20),
                UtcTime: DateTime.UtcNow,
                MarketStatus: "open");

            await Task.Delay(_options.PollIntervalMs, cancellationToken);
        }
    }
}
