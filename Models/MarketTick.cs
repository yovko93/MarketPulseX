namespace MarketPulseX.Models
{
    //public sealed record MarketTick(
    // string Symbol,
    // decimal Price,
    // int Size,
    // DateTime UtcTime);

    public sealed record MarketTick(
    string Symbol,
    decimal Price,
    int Size,
    DateTime UtcTime,
    string? MarketStatus = null,
    int[]? ConditionCodes = null,
    bool? DarkPool = null);
}
