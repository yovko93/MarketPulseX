namespace MarketPulseX.Services.Ingestion;

public sealed class MarketDataAdapterSelector : IMarketDataAdapterSelector
{
    private readonly IReadOnlyDictionary<string, IMarketDataAdapter> _adapters;

    public MarketDataAdapterSelector(IEnumerable<IMarketDataAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(
            adapter => adapter.FeedName,
            adapter => adapter,
            StringComparer.OrdinalIgnoreCase);
    }

    public IMarketDataAdapter Select(string adapterName)
    {
        if (_adapters.TryGetValue(adapterName, out var adapter))
            return adapter;

        var configured = string.IsNullOrWhiteSpace(adapterName)
            ? "<empty>"
            : adapterName;

        var supported = string.Join(", ", _adapters.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));

        throw new InvalidOperationException(
            $"Unknown market data adapter '{configured}'. Supported: {supported}");
    }
}
