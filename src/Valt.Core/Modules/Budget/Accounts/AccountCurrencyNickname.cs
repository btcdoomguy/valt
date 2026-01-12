using Valt.Core.Common.Exceptions;

namespace Valt.Core.Modules.Budget.Accounts;

public record AccountCurrencyNickname
{
    public string Value { get; }

    private AccountCurrencyNickname(string value)
    {
        Value = value;
    }
    
    public static AccountCurrencyNickname Empty => new(string.Empty);

    public static AccountCurrencyNickname New(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Empty;
        
        if (value.Length > 15)
            throw new MaximumFieldLengthException(nameof(AccountCurrencyNickname), 15);

        return new AccountCurrencyNickname(value);
    }

    public static implicit operator string(AccountCurrencyNickname name) => name.Value;

    public static implicit operator AccountCurrencyNickname(string name) => New(name);
}