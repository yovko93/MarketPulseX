using MarketPulseX.Models;

namespace MarketPulseX.Services.Ingestion;

public interface IMarketDataAdapter
{
    string FeedName { get; }

    IAsyncEnumerable<MarketTick> StreamAsync(CancellationToken cancellationToken);
}
