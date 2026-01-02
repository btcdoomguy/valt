using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;
using Valt.Core.Modules.AvgPrice.Exceptions;
using Valt.Infra.DataAccess;

namespace Valt.Infra.Modules.AvgPrice;

internal sealed class AvgPriceTotalizer : IAvgPriceTotalizer
{
    private readonly ILocalDatabase _localDatabase;

    public AvgPriceTotalizer(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<IAvgPriceTotalizer.TotalsDTO> GetTotalsAsync(int year, IEnumerable<AvgPriceProfileId> profileIds)
    {
        var profileIdList = profileIds.ToList();

        if (profileIdList.Count == 0)
        {
            return Task.FromResult(CreateEmptyResult(year));
        }

        var profiles = GetAndValidateProfiles(profileIdList);
        var allLines = GetAllLinesForProfiles(profileIdList);
        var calculatedLines = CalculateProfitLossForLines(allLines, profiles);
        var result = AggregateByYear(year, calculatedLines);

        return Task.FromResult(result);
    }

    private IAvgPriceTotalizer.TotalsDTO CreateEmptyResult(int year)
    {
        var emptyValues = new IAvgPriceTotalizer.ValuesDTO(0, 0, 0, 0);
        var monthlyTotals = Enumerable.Range(1, 12)
            .Select(month => new IAvgPriceTotalizer.MonthlyTotalsDTO(new DateTime(year, month, 1), emptyValues))
            .ToList();

        return new IAvgPriceTotalizer.TotalsDTO(year, monthlyTotals, emptyValues);
    }

    private List<AvgPriceProfileEntity> GetAndValidateProfiles(IEnumerable<AvgPriceProfileId> profileIds)
    {
        var objectIds = profileIds.Select(id => new ObjectId(id.ToString())).ToList();
        var profiles = _localDatabase.GetAvgPriceProfiles()
            .Find(p => objectIds.Contains(p.Id))
            .ToList();

        if (profiles.Count == 0)
        {
            return profiles;
        }

        var distinctCurrencies = profiles.Select(p => p.Currency).Distinct().ToList();
        if (distinctCurrencies.Count > 1)
        {
            throw new MixedCurrencyException();
        }

        return profiles;
    }

    private List<LineWithProfile> GetAllLinesForProfiles(IEnumerable<AvgPriceProfileId> profileIds)
    {
        var objectIds = profileIds.Select(id => new ObjectId(id.ToString())).ToList();

        return _localDatabase.GetAvgPriceLines()
            .Find(line => objectIds.Contains(line.ProfileId))
            .Select(line => new LineWithProfile(line, line.ProfileId))
            .OrderBy(x => x.Line.Date)
            .ThenBy(x => x.Line.DisplayOrder)
            .ToList();
    }

    private List<CalculatedLine> CalculateProfitLossForLines(List<LineWithProfile> lines, List<AvgPriceProfileEntity> profiles)
    {
        var result = new List<CalculatedLine>();
        var profileAvgCosts = profiles.ToDictionary(p => p.Id, _ => 0m);
        var profileQuantities = profiles.ToDictionary(p => p.Id, _ => 0m);

        foreach (var lineWithProfile in lines)
        {
            var line = lineWithProfile.Line;
            var profileId = lineWithProfile.ProfileId;
            var profitLoss = 0m;

            var currentAvgCost = profileAvgCosts[profileId];
            var currentQuantity = profileQuantities[profileId];

            var lineType = (AvgPriceLineTypes)line.AvgPriceLineTypeId;

            switch (lineType)
            {
                case AvgPriceLineTypes.Buy:
                    var newTotalCost = (currentQuantity * currentAvgCost) + line.Amount;
                    var newQuantity = currentQuantity + line.Quantity;
                    profileAvgCosts[profileId] = newQuantity > 0 ? newTotalCost / newQuantity : 0m;
                    profileQuantities[profileId] = newQuantity;
                    break;

                case AvgPriceLineTypes.Sell:
                    var costBasis = line.Quantity * currentAvgCost;
                    profitLoss = line.Amount - costBasis;
                    profileQuantities[profileId] = currentQuantity - line.Quantity;
                    break;

                case AvgPriceLineTypes.Setup:
                    profileQuantities[profileId] = line.Quantity;
                    profileAvgCosts[profileId] = line.Quantity > 0 ? line.Amount / line.Quantity : 0m;
                    break;
            }

            result.Add(new CalculatedLine(
                DateOnly.FromDateTime(line.Date),
                lineType,
                line.Amount,
                line.Quantity,
                profitLoss));
        }

        return result;
    }

    private IAvgPriceTotalizer.TotalsDTO AggregateByYear(int year, List<CalculatedLine> lines)
    {
        var yearLines = lines.Where(l => l.Date.Year == year).ToList();

        var monthlyTotals = Enumerable.Range(1, 12)
            .Select(month =>
            {
                var monthLines = yearLines.Where(l => l.Date.Month == month).ToList();
                return new IAvgPriceTotalizer.MonthlyTotalsDTO(
                    new DateTime(year, month, 1),
                    CalculateValues(monthLines));
            })
            .ToList();

        var yearlyValues = CalculateValues(yearLines);

        return new IAvgPriceTotalizer.TotalsDTO(year, monthlyTotals, yearlyValues);
    }

    private static IAvgPriceTotalizer.ValuesDTO CalculateValues(List<CalculatedLine> lines)
    {
        var amountBought = lines
            .Where(l => l.Type == AvgPriceLineTypes.Buy || l.Type == AvgPriceLineTypes.Setup)
            .Sum(l => l.Amount);

        var amountSold = lines
            .Where(l => l.Type == AvgPriceLineTypes.Sell)
            .Sum(l => l.Amount);

        var totalProfitLoss = lines.Sum(l => l.ProfitLoss);

        var volume = lines.Sum(l => l.Amount);

        return new IAvgPriceTotalizer.ValuesDTO(amountBought, amountSold, totalProfitLoss, volume);
    }

    private record LineWithProfile(AvgPriceLineEntity Line, ObjectId ProfileId);

    private record CalculatedLine(DateOnly Date, AvgPriceLineTypes Type, decimal Amount, decimal Quantity, decimal ProfitLoss);
}
