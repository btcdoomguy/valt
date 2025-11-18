namespace Valt.Infra.Modules.Budget.Transactions;

public enum TransactionEntityType
{
    Fiat = 0,
    Bitcoin = 1,
    FiatToFiat = 2,
    BitcoinToBitcoin = 3,
    FiatToBitcoin = 4,
    BitcoinToFiat = 5
}