using System.Globalization;
using System.Text.Json;
using MarketPulseX.Hubs;
using MarketPulseX.Models;
using Microsoft.AspNetCore.SignalR;

namespace MarketPulseX.Services;

public sealed class CryptoApisExchangeRateWorker : BackgroundService
{
    private readonly MarketState _state;
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly ILogger<CryptoApisExchangeRateWorker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly string _apiKey;
    private readonly string _fromAssetSymbol;
    private readonly string _toAssetSymbol;
    private readonly int _pollIntervalMs;

    public CryptoApisExchangeRateWorker(
        MarketState state,
        IHubContext<MarketHub> hubContext,
        ILogger<CryptoApisExchangeRateWorker> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _state = state;
        _hubContext = hubContext;
        _logger = logger;
        _httpClientFactory = httpClientFactory;

        _apiKey = configuration["CryptoApis:ApiKey"]
            ?? throw new InvalidOperationException("Missing configuration: CryptoApis:ApiKey");

        _fromAssetSymbol = configuration["CryptoApis:FromAssetSymbol"] ?? "BTC";
        _toAssetSymbol = configuration["CryptoApis:ToAssetSymbol"] ?? "USD";

        _pollIntervalMs = configuration.GetValue<int?>("CryptoApis:PollIntervalMs") ?? 1000;
        _pollIntervalMs = Math.Clamp(_pollIntervalMs, 250, 60_000);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "CryptoAPIs exchange rate worker started for {From}/{To}. Poll interval: {PollIntervalMs} ms",
            _fromAssetSymbol,
            _toAssetSymbol,
            _pollIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tick = await FetchTickAsync(stoppingToken);

                if (tick is not null)
                {
                    _state.Add(tick);
                    await _hubContext.Clients.All.SendAsync("tick", tick, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pull CryptoAPIs exchange rate.");
            }

            await Task.Delay(_pollIntervalMs, stoppingToken);
        }
    }

    private async Task<MarketTick?> FetchTickAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(nameof(CryptoApisExchangeRateWorker));

        var endpoint =
            $"market-data/exchange-rates/by-symbol/{Uri.EscapeDataString(_fromAssetSymbol)}/{Uri.EscapeDataString(_toAssetSymbol)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("X-API-Key", _apiKey);

        using var response = await client.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "CryptoAPIs returned non-success status {StatusCode}. Body: {Body}",
                (int)response.StatusCode,
                body);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("data", out var dataElement) ||
            !dataElement.TryGetProperty("item", out var itemElement))
        {
            _logger.LogWarning("CryptoAPIs response did not include data.item.");
            return null;
        }

        if (!itemElement.TryGetProperty("rate", out var rateElement))
        {
            _logger.LogWarning("CryptoAPIs response did not include item.rate.");
            return null;
        }

        var rateText = rateElement.GetString();

        if (!decimal.TryParse(rateText, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate))
        {
            _logger.LogWarning("Could not parse CryptoAPIs rate value: {RateText}", rateText);
            return null;
        }

        var tickUtc = DateTime.UtcNow;
        if (itemElement.TryGetProperty("calculationTimestamp", out var calcTimestampElement))
        {
            var timestamp = calcTimestampElement.GetInt64();
            tickUtc = timestamp > 1_000_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime
                : DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }

        return new MarketTick(
            Symbol: $"{_fromAssetSymbol}/{_toAssetSymbol}",
            Price: rate,
            Size: 1,
            UtcTime: tickUtc,
            MarketStatus: "live");
    }
}
