using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Budget.Accounts.Exceptions;

namespace Valt.Core.Modules.Budget.Accounts;

public record AccountName
{
    public string Value { get; }

    private AccountName(string value)
    {
        Value = value;
    }

    public static AccountName New(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new EmptyAccountNameException();

        if (value.Length > 30)
            throw new MaximumFieldLengthException(nameof(AccountName), 30);

        return new AccountName(value);
    }

    public static implicit operator string(AccountName name) => name.Value;

    public static implicit operator AccountName(string name) => New(name);
}