using Valt.Core.Common;

namespace Valt.Infra.Modules.Budget.Transactions;

public static class DatabaseValueParser
{
    public static BtcValue AdaptToBtcValue(long storedValue)
    {
        return storedValue < 0
            ? storedValue * -1
            : storedValue;
    }

    public static FiatValue AdaptToFiatValue(decimal storedValue)
    {
        return storedValue < 0
            ? storedValue * -1
            : storedValue;
    }
}