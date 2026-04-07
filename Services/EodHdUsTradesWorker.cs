using MarketPulseX.Converters;
using MarketPulseX.Hubs;
using MarketPulseX.Models;
using Microsoft.AspNetCore.SignalR;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarketPulseX.Services;

public sealed class EodHdUsTradesWorker : BackgroundService
{
    private readonly MarketState _state;
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly ILogger<EodHdUsTradesWorker> _logger;
    private readonly string _apiToken;
    private readonly string[] _symbols;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EodHdUsTradesWorker(
        MarketState state,
        IHubContext<MarketHub> hubContext,
        ILogger<EodHdUsTradesWorker> logger,
        IConfiguration configuration)
    {
        _state = state;
        _hubContext = hubContext;
        _logger = logger;

        _apiToken = configuration["EodHd:ApiToken"]
            ?? throw new InvalidOperationException("Missing configuration: EodHd:ApiToken");

        _symbols = (configuration["EodHd:Symbols"] ?? "TSLA")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (_symbols.Length == 0)
            throw new InvalidOperationException("No symbols configured in EodHd:Symbols");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSocketAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EODHD socket failed. Reconnecting in 3 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private async Task RunSocketAsync(CancellationToken stoppingToken)
    {
        var uri = new Uri(
            $"wss://ws.eodhistoricaldata.com/ws/us?api_token={Uri.EscapeDataString(_apiToken)}");

        using var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

        await socket.ConnectAsync(uri, stoppingToken);
        _logger.LogInformation("Connected to EODHD US trades feed.");

        var subscribePayload = JsonSerializer.Serialize(new
        {
            action = "subscribe",
            symbols = string.Join(",", _symbols)
        });

        await SendTextAsync(socket, subscribePayload, stoppingToken);
        _logger.LogInformation("Subscribed to symbols: {Symbols}", string.Join(", ", _symbols));

        var buffer = new byte[16 * 1024];
        var messageBuilder = new StringBuilder();

        while (socket.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                stoppingToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogWarning("Socket closed by server. Status: {Status}, Description: {Description}",
                    socket.CloseStatus, socket.CloseStatusDescription);
                break;
            }

            if (result.MessageType != WebSocketMessageType.Text)
                continue;

            messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (!result.EndOfMessage)
                continue;

            var json = messageBuilder.ToString();
            messageBuilder.Clear();

            if (string.IsNullOrWhiteSpace(json))
                continue;

            EodHdTradeMessage? trade;

            try
            {
                trade = JsonSerializer.Deserialize<EodHdTradeMessage>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Could not parse message: {Json}", json);
                continue;
            }

            if (trade is null || string.IsNullOrWhiteSpace(trade.Symbol))
                continue;

            var tick = new MarketTick(
                Symbol: trade.Symbol,
                Price: trade.Price,
                Size: trade.Volume > int.MaxValue ? int.MaxValue : (int)trade.Volume,
                UtcTime: DateTimeOffset.FromUnixTimeMilliseconds(trade.Timestamp).UtcDateTime,
                MarketStatus: trade.MarketStatus,
                ConditionCodes: trade.ConditionCodes,
                DarkPool: trade.DarkPool);

            _state.Add(tick);

            await _hubContext.Clients.All.SendAsync("tick", tick, stoppingToken);
        }

        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", stoppingToken);
        }
    }

    private static Task SendTextAsync(
        ClientWebSocket socket,
        string text,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(text);

        return socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }

    private sealed class EodHdTradeMessage
    {
        [JsonPropertyName("s")]
        public string Symbol { get; init; } = string.Empty;

        [JsonPropertyName("p")]
        public decimal Price { get; init; }

        [JsonPropertyName("v")]
        public long Volume { get; init; }

        [JsonPropertyName("c")]
        [JsonConverter(typeof(IntArrayOrSingleConverter))]
        public int[]? ConditionCodes { get; init; }

        [JsonPropertyName("dp")]
        public bool? DarkPool { get; init; }

        [JsonPropertyName("ms")]
        public string? MarketStatus { get; init; }

        [JsonPropertyName("t")]
        public long Timestamp { get; init; }
    }
}