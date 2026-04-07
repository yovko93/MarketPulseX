using MarketPulseX.Models;

namespace MarketPulseX.Services.Ingestion.Adapters;

public sealed class RithmicMarketDataAdapter : IMarketDataAdapter
{
    private readonly ILogger<RithmicMarketDataAdapter> _logger;

    public RithmicMarketDataAdapter(ILogger<RithmicMarketDataAdapter> logger)
    {
        _logger = logger;
    }

    public string FeedName => "Rithmic";

    public async IAsyncEnumerable<MarketTick> StreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Adapter} adapter is a placeholder. No ticks will be emitted.", FeedName);

        while (!cancellationToken.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        yield break;
    }
}
