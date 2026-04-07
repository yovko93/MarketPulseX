using MarketPulseX.Hubs;
using MarketPulseX.Services;
using MarketPulseX.Services.Ingestion;
using MarketPulseX.Services.Ingestion.Adapters;
using MarketPulseX.Services.Ingestion.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MarketState>();
builder.Services.Configure<IngestionOptions>(
    builder.Configuration.GetSection(IngestionOptions.SectionName));

builder.Services.AddSingleton<IMarketDataAdapter, TradovateMarketDataAdapter>();
builder.Services.AddSingleton<IMarketDataAdapter, RithmicMarketDataAdapter>();
builder.Services.AddSingleton<IMarketDataAdapter, CmeMdpMarketDataAdapter>();
builder.Services.AddSingleton<IMarketDataAdapterSelector, MarketDataAdapterSelector>();

builder.Services.AddHostedService<MarketDataIngestionWorker>();
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
