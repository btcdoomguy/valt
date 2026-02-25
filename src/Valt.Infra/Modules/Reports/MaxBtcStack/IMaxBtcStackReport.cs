namespace Valt.Infra.Modules.Reports.MaxBtcStack;

public interface IMaxBtcStackReport
{
    Task<MaxBtcStackData> GetAsync(long currentStackInSats, IReportDataProvider provider);
}
