namespace MarketPulseX.Services.Ingestion.Options;

public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    public string Adapter { get; init; } = "Tradovate";

    public string Symbol { get; init; } = "NQ";

    public int PollIntervalMs { get; init; } = 100;
}
