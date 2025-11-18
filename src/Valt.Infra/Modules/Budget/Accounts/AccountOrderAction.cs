using Valt.Core.Modules.Budget.Accounts;

namespace Valt.Infra.Modules.Budget.Accounts;

public record AccountOrderAction(AccountId AccountId, bool Up);