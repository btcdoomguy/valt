using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.AvgPrice.Events;

public sealed record AvgPriceLineUpdatedEvent(AvgPriceLine AvgPriceLine) : IDomainEvent;