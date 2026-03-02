using NSubstitute;
using Valt.App.Modules.AvgPrice.Contracts;
using Valt.App.Modules.AvgPrice.DTOs;
using Valt.App.Modules.Budget.Accounts.Contracts;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Categories.Contracts;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Transactions.Contracts;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Modules.AvgPrice;
using Valt.Infra.Services.CsvExport;

namespace Valt.Tests.CsvExport;

[TestFixture]
public class CsvExportAvgPriceLinesTests
{
    private IAvgPriceQueries _avgPriceQueries = null!;
    private CsvExportService _service = null!;
    private const string ProfileId = "507f1f77bcf86cd799439011";

    [SetUp]
    public void SetUp()
    {
        _avgPriceQueries = Substitute.For<IAvgPriceQueries>();

        var transactionQueries = Substitute.For<ITransactionQueries>();
        var accountQueries = Substitute.For<IAccountQueries>();
        var categoryQueries = Substitute.For<ICategoryQueries>();

        transactionQueries.GetTransactionsAsync(Arg.Any<TransactionQueryFilter>())
            .Returns(Task.FromResult(new TransactionsDTO([])));
        accountQueries.GetAccountsAsync(Arg.Any<bool>())
            .Returns(Task.FromResult<IEnumerable<AccountDTO>>([]));
        categoryQueries.GetCategoriesAsync()
            .Returns(Task.FromResult(new CategoriesDTO([])));

        _service = new CsvExportService(transactionQueries, accountQueries, categoryQueries, _avgPriceQueries);
    }

    private static AvgPriceLineDTO MakeLine(
        string id,
        DateOnly date,
        AvgPriceLineTypes type,
        decimal quantity,
        decimal amount,
        decimal totalQuantity,
        decimal totalCost,
        decimal avgCost,
        string comment = "")
    {
        return new AvgPriceLineDTO(
            id,
            date,
            DisplayOrder: 0,
            AvgPriceLineTypeId: (int)type,
            Quantity: quantity,
            Amount: amount,
            Comment: comment,
            AvgCostOfAcquisition: avgCost,
            TotalCost: totalCost,
            TotalQuantity: totalQuantity);
    }

    private static string[] ParseLines(string csv) =>
        csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static string[] SplitFields(string csvLine) =>
        csvLine.TrimEnd('\r').Split(',');

    [Test]
    public async Task Should_Export_Header_Row()
    {
        _avgPriceQueries.GetLinesOfProfileAsync(Arg.Any<AvgPriceProfileId>())
            .Returns(Array.Empty<AvgPriceLineDTO>());

        var csv = await _service.ExportAvgPriceLinesAsync(ProfileId);
        var lines = ParseLines(csv);

        Assert.That(lines, Has.Length.GreaterThanOrEqualTo(1));
        var header = SplitFields(lines[0]);
        Assert.That(header[0], Is.EqualTo("date"));
        Assert.That(header[1], Is.EqualTo("type"));
        Assert.That(header[2], Is.EqualTo("quantity"));
        Assert.That(header[3], Is.EqualTo("unit_price"));
        Assert.That(header[4], Is.EqualTo("amount"));
        Assert.That(header[5], Is.EqualTo("total_quantity"));
        Assert.That(header[6], Is.EqualTo("total_cost"));
        Assert.That(header[7], Is.EqualTo("avg_cost"));
        Assert.That(header[8], Is.EqualTo("comment"));
    }

    [Test]
    public async Task Should_Export_Buy_Line()
    {
        var line = MakeLine("id1", new DateOnly(2024, 3, 15), AvgPriceLineTypes.Buy,
            quantity: 0.5m, amount: 25000m, totalQuantity: 0.5m, totalCost: 25000m, avgCost: 50000m, comment: "First buy");

        _avgPriceQueries.GetLinesOfProfileAsync(Arg.Any<AvgPriceProfileId>())
            .Returns(new[] { line });

        var csv = await _service.ExportAvgPriceLinesAsync(ProfileId);
        var lines = ParseLines(csv);

        Assert.That(lines, Has.Length.EqualTo(2));
        var fields = SplitFields(lines[1]);
        Assert.That(fields[0], Is.EqualTo("2024-03-15"));
        Assert.That(fields[1], Is.EqualTo("Buy"));
        Assert.That(fields[2], Is.EqualTo("0.50000000"));
        Assert.That(fields[3], Is.EqualTo("50000.00")); // unit_price = amount/quantity
        Assert.That(fields[4], Is.EqualTo("25000.00"));
        Assert.That(fields[5], Is.EqualTo("0.50000000"));
        Assert.That(fields[6], Is.EqualTo("25000.00"));
        Assert.That(fields[7], Is.EqualTo("50000.00"));
        Assert.That(fields[8], Is.EqualTo("First buy"));
    }

    [Test]
    public async Task Should_Export_Sell_Line()
    {
        var line = MakeLine("id2", new DateOnly(2024, 6, 1), AvgPriceLineTypes.Sell,
            quantity: 0.1m, amount: 6000m, totalQuantity: 0.4m, totalCost: 20000m, avgCost: 50000m);

        _avgPriceQueries.GetLinesOfProfileAsync(Arg.Any<AvgPriceProfileId>())
            .Returns(new[] { line });

        var csv = await _service.ExportAvgPriceLinesAsync(ProfileId);
        var lines = ParseLines(csv);

        Assert.That(lines, Has.Length.EqualTo(2));
        var fields = SplitFields(lines[1]);
        Assert.That(fields[1], Is.EqualTo("Sell"));
    }

    [Test]
    public async Task Should_Export_Setup_Line()
    {
        var line = MakeLine("id3", new DateOnly(2023, 1, 1), AvgPriceLineTypes.Setup,
            quantity: 1.0m, amount: 30000m, totalQuantity: 1.0m, totalCost: 30000m, avgCost: 30000m);

        _avgPriceQueries.GetLinesOfProfileAsync(Arg.Any<AvgPriceProfileId>())
            .Returns(new[] { line });

        var csv = await _service.ExportAvgPriceLinesAsync(ProfileId);
        var lines = ParseLines(csv);

        Assert.That(lines, Has.Length.EqualTo(2));
        var fields = SplitFields(lines[1]);
        Assert.That(fields[1], Is.EqualTo("Setup"));
    }

    [Test]
    public async Task Should_Export_Multiple_Lines_In_Order()
    {
        var line1 = MakeLine("id1", new DateOnly(2024, 1, 1), AvgPriceLineTypes.Buy,
            0.5m, 25000m, 0.5m, 25000m, 50000m);
        var line2 = MakeLine("id2", new DateOnly(2024, 2, 1), AvgPriceLineTypes.Buy,
            0.5m, 30000m, 1.0m, 55000m, 55000m);
        var line3 = MakeLine("id3", new DateOnly(2024, 3, 1), AvgPriceLineTypes.Sell,
            0.2m, 14000m, 0.8m, 44000m, 55000m);

        _avgPriceQueries.GetLinesOfProfileAsync(Arg.Any<AvgPriceProfileId>())
            .Returns(new[] { line1, line2, line3 });

        var csv = await _service.ExportAvgPriceLinesAsync(ProfileId);
        var lines = ParseLines(csv);

        Assert.That(lines, Has.Length.EqualTo(4)); // header + 3 data rows
        Assert.That(SplitFields(lines[1])[0], Is.EqualTo("2024-01-01"));
        Assert.That(SplitFields(lines[2])[0], Is.EqualTo("2024-02-01"));
        Assert.That(SplitFields(lines[3])[0], Is.EqualTo("2024-03-01"));
        Assert.That(SplitFields(lines[3])[1], Is.EqualTo("Sell"));
    }

    [Test]
    public async Task Should_Export_Empty_Comment_As_Empty_String()
    {
        var line = MakeLine("id1", new DateOnly(2024, 1, 1), AvgPriceLineTypes.Buy,
            0.5m, 25000m, 0.5m, 25000m, 50000m, comment: "");

        _avgPriceQueries.GetLinesOfProfileAsync(Arg.Any<AvgPriceProfileId>())
            .Returns(new[] { line });

        var csv = await _service.ExportAvgPriceLinesAsync(ProfileId);
        var lines = ParseLines(csv);

        Assert.That(lines, Has.Length.EqualTo(2));
        var fields = SplitFields(lines[1]);
        // 9 fields: date,type,quantity,unit_price,amount,total_quantity,total_cost,avg_cost,comment
        Assert.That(fields, Has.Length.EqualTo(9));
        Assert.That(fields[8], Is.EqualTo(string.Empty));
    }
}
