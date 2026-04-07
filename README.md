# MarketPulseX

.NET 8 skeleton with Aurora-style ingestion abstraction.

## Ingestion adapters

Configure active feed in `appsettings.json`:

```json
"Ingestion": {
  "Adapter": "Tradovate",
  "Symbol": "NQ",
  "PollIntervalMs": 100
}
```

Supported adapter names:

- `Tradovate` (sample streaming implementation)
- `Rithmic` (placeholder)
- `CmeMdp` (placeholder)

The strategy pipeline (`MarketState` + SignalR fanout) remains unchanged when adapter is switched.
