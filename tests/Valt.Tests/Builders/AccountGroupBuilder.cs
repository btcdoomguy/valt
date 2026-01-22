using LiteDB;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Tests.Builders;

/// <summary>
/// Builder for creating AccountGroup test data.
/// Supports both fluent API (preferred) and property initialization syntax.
/// </summary>
public class AccountGroupBuilder
{
    private AccountGroupId _id = new();
    private AccountGroupName _name = "Test Group";
    private int _displayOrder = 0;
    private int _version = 1;

    // Public properties for backward compatibility with property initializer syntax
    public AccountGroupId Id { get => _id; set => _id = value; }
    public AccountGroupName Name { get => _name; set => _name = value; }
    public int DisplayOrder { get => _displayOrder; set => _displayOrder = value; }
    public int Version { get => _version; set => _version = value; }

    public static AccountGroupBuilder AGroup() => new();

    public AccountGroupBuilder WithId(AccountGroupId id)
    {
        _id = id;
        return this;
    }

    public AccountGroupBuilder WithName(string name)
    {
        _name = AccountGroupName.New(name);
        return this;
    }

    public AccountGroupBuilder WithDisplayOrder(int displayOrder)
    {
        _displayOrder = displayOrder;
        return this;
    }

    public AccountGroupBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public AccountGroupEntity BuildEntity()
    {
        return new AccountGroupEntity
        {
            Id = _id.ToObjectId(),
            Name = _name,
            DisplayOrder = _displayOrder,
            Version = _version
        };
    }

    public AccountGroup Build()
    {
        return AccountGroup.Create(_id, _name, _displayOrder, _version);
    }
}
