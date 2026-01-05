namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;

/// <summary>
/// Sent when fiat history needs to be refreshed, e.g., when a transaction
/// with a date earlier than the minimum fiat date is saved.
/// </summary>
public record FiatHistoryRefreshRequestedMessage();
