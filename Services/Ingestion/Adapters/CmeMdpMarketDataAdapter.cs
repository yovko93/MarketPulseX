using MarketPulseX.Models;

namespace MarketPulseX.Services.Ingestion.Adapters;

public sealed class CmeMdpMarketDataAdapter : IMarketDataAdapter
{
    private readonly ILogger<CmeMdpMarketDataAdapter> _logger;

    public CmeMdpMarketDataAdapter(ILogger<CmeMdpMarketDataAdapter> logger)
    {
        _logger = logger;
    }

    public string FeedName => "CmeMdp";

    public async IAsyncEnumerable<MarketTick> StreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Adapter} adapter is a placeholder. No ticks will be emitted.", FeedName);

        while (!cancellationToken.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        yield break;
    }
}
