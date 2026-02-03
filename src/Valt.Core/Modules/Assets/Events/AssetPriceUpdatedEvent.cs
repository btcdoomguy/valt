using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.Assets.Events;

public sealed record AssetPriceUpdatedEvent(Asset Asset, decimal OldPrice, decimal NewPrice) : IDomainEvent;
