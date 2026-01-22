using Valt.Core.Kernel.Abstractions;

namespace Valt.Core.Modules.Budget.Accounts.Contracts;

public interface IAccountGroupRepository : IRepository
{
    Task<AccountGroup?> GetByIdAsync(AccountGroupId id);
    Task<IList<AccountGroup>> GetAllAsync();
    Task SaveAsync(AccountGroup group);
    Task DeleteAsync(AccountGroupId id);
}
