using Valt.Core.Modules.Budget.Accounts.Exceptions;

namespace Valt.Core.Modules.Budget.Accounts;

public record AccountGroupName
{
    public string Value { get; }

    private AccountGroupName(string value)
    {
        Value = value;
    }

    public static AccountGroupName New(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new EmptyAccountGroupNameException();

        if (value.Length > 50)
            throw new AccountGroupNameLengthException();

        return new AccountGroupName(value);
    }

    public static implicit operator string(AccountGroupName name) => name.Value;

    public static implicit operator AccountGroupName(string name) => AccountGroupName.New(name);
}
