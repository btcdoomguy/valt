using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.Assets.Events;

public sealed record AssetDeletedEvent(Asset Asset) : IDomainEvent;
