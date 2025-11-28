using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports.AllTimeHigh;

public interface IAllTimeHighReport
{
    Task<AllTimeHighData> GetAsync(FiatCurrency currency);
}