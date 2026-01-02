using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Tests.Builders;

/// <summary>
/// Builder for creating FiatAccount test data.
/// Supports both fluent API (preferred) and property initialization syntax.
/// </summary>
public class FiatAccountBuilder
{
    private AccountId _id = new();
    private AccountName _name = "Test Account";
    private FiatCurrency _fiatCurrency = FiatCurrency.Usd;
    private Icon _icon = Icon.Empty;
    private FiatValue _value = FiatValue.Empty;
    private bool _visible = true;
    private int _version = 1;

    // Public properties for backward compatibility with property initializer syntax
    public AccountId Id { get => _id; set => _id = value; }
    public AccountName Name { get => _name; set => _name = value; }
    public FiatCurrency FiatCurrency { get => _fiatCurrency; set => _fiatCurrency = value; }
    public Icon Icon { get => _icon; set => _icon = value; }
    public FiatValue Value { get => _value; set => _value = value; }
    public bool Visible { get => _visible; set => _visible = value; }
    public int Version { get => _version; set => _version = value; }

    public static FiatAccountBuilder AnAccount() => new();

    public FiatAccountBuilder WithId(AccountId id)
    {
        _id = id;
        return this;
    }

    public FiatAccountBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public FiatAccountBuilder WithFiatCurrency(FiatCurrency currency)
    {
        _fiatCurrency = currency;
        return this;
    }

    public FiatAccountBuilder WithIcon(Icon icon)
    {
        _icon = icon;
        return this;
    }

    public FiatAccountBuilder WithValue(decimal value)
    {
        _value = FiatValue.New(value);
        return this;
    }

    public FiatAccountBuilder WithValue(FiatValue value)
    {
        _value = value;
        return this;
    }

    public FiatAccountBuilder WithVisible(bool visible)
    {
        _visible = visible;
        return this;
    }

    public FiatAccountBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public AccountEntity Build()
    {
        return new AccountEntity()
        {
            Id = _id.ToObjectId(),
            Currency = _fiatCurrency.Code,
            InitialAmount = _value,
            Icon = _icon.ToString(),
            Name = _name,
            AccountEntityType = AccountEntityType.Fiat,
            Version = _version,
            Visible = _visible
        };
    }
}
