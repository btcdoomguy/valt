using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Modules.Assets.Events;

public sealed record AssetCreatedEvent(Asset Asset) : IDomainEvent;
