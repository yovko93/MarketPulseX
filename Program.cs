using MarketPulseX.Hubs;
using MarketPulseX.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MarketState>();
builder.Services.AddHttpClient(nameof(CryptoApisExchangeRateWorker), client =>
{
    client.BaseAddress = new Uri("https://rest.cryptoapis.io/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
//builder.Services.AddHostedService<FakeFeedWorker>();
//builder.Services.AddHostedService<EodHdUsTradesWorker>();
builder.Services.AddHostedService<CryptoApisExchangeRateWorker>();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", (MarketState state) =>
{
    return Results.Ok(state.GetStatus());
});

app.MapGet("/api/latest", (MarketState state, int take = 100) =>
{
    take = Math.Clamp(take, 1, 500);
    return Results.Ok(state.GetLatest(take));
});

app.MapHub<MarketHub>("/hubs/market");

app.Run();
