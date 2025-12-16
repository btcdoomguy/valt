using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.AvgPrice.Events;

public sealed record AvgPriceProfileDeletedEvent(AvgPriceProfile AvgPriceProfile) : IDomainEvent;