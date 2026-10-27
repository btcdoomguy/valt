using Valt.App.Kernel.Notifications;

namespace Valt.Infra.Modules.Reports.Notifications;

/// <summary>
/// Published whenever a transaction is saved or deleted. Consumed by the report
/// data provider cache so that edits without a transaction-count change (e.g. an
/// exclusion-flag toggle) still invalidate cached report data.
/// </summary>
public sealed record TransactionsDataChangedNotification : INotification;
