namespace MarketPulseX.Models
{
    public sealed record MarketTick(
     string Symbol,
     decimal Price,
     int Size,
     DateTime UtcTime);
}
