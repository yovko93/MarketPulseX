using MarketPulseX.Hubs;
using MarketPulseX.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MarketState>();
builder.Services.AddHostedService<FakeFeedWorker>();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();


app.MapGet("/", () => Results.Ok(new { Name = "MarketPulseX", Status = "Running" }));

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