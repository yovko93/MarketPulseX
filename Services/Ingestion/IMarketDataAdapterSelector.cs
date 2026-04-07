namespace MarketPulseX.Services.Ingestion;

public interface IMarketDataAdapterSelector
{
    IMarketDataAdapter Select(string adapterName);
}
