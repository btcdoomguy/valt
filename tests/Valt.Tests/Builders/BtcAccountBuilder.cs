using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra.Modules.Budget.Accounts;

namespace Valt.Tests.Builders;

/// <summary>
/// Builder for creating BtcAccount (Bitcoin account) test data.
/// Supports both fluent API (preferred) and property initialization syntax.
/// </summary>
public class BtcAccountBuilder
{
    private AccountId _id = new();
    private AccountName _name = "Test BTC Account";
    private Icon _icon = Icon.Empty;
    private BtcValue _value = BtcValue.Empty;
    private bool _visible = true;
    private int _version = 1;

    // Public properties for backward compatibility with property initializer syntax
    public AccountId Id { get => _id; set => _id = value; }
    public AccountName Name { get => _name; set => _name = value; }
    public Icon Icon { get => _icon; set => _icon = value; }
    public BtcValue Value { get => _value; set => _value = value; }
    public bool Visible { get => _visible; set => _visible = value; }
    public int Version { get => _version; set => _version = value; }

    public static BtcAccountBuilder AnAccount() => new();

    public BtcAccountBuilder WithId(AccountId id)
    {
        _id = id;
        return this;
    }

    public BtcAccountBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public BtcAccountBuilder WithIcon(Icon icon)
    {
        _icon = icon;
        return this;
    }

    public BtcAccountBuilder WithValue(BtcValue value)
    {
        _value = value;
        return this;
    }

    public BtcAccountBuilder WithSats(long sats)
    {
        _value = BtcValue.New(sats);
        return this;
    }

    public BtcAccountBuilder WithBitcoin(decimal btc)
    {
        _value = BtcValue.ParseBitcoin(btc);
        return this;
    }

    public BtcAccountBuilder WithVisible(bool visible)
    {
        _visible = visible;
        return this;
    }

    public BtcAccountBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public AccountEntity Build()
    {
        return new AccountEntity()
        {
            Id = new ObjectId(_id),
            Currency = null,
            InitialAmount = _value.Sats,
            Icon = _icon.ToString(),
            Name = _name,
            AccountEntityType = AccountEntityType.Bitcoin,
            Version = _version,
            Visible = _visible
        };
    }
}
