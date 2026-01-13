using Valt.Infra.Services.CsvImport;

namespace Valt.Tests.CsvImport;

/// <summary>
/// Tests for the CsvTemplateGenerator service.
/// Verifies template structure and content.
/// </summary>
[TestFixture]
public class CsvTemplateGeneratorTests
{
    private ICsvTemplateGenerator _generator = null!;
    private ICsvImportParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _generator = new CsvTemplateGenerator();
        _parser = new CsvImportParser();
    }

    #region Template Structure Tests

    [Test]
    public void Should_Generate_Template_With_Header_Row()
    {
        // Act
        var template = _generator.GenerateTemplate();

        // Assert
        Assert.That(template, Does.StartWith("date,description,amount,account,to_account,to_amount,category"));
    }

    [Test]
    public void Should_Generate_Template_With_All_Transaction_Types()
    {
        // Act
        var template = _generator.GenerateTemplate();

        // Assert: Should contain examples of all transaction patterns

        // FiatDetails - Expense (negative fiat)
        Assert.That(template, Does.Contain("Grocery shopping"));
        Assert.That(template, Does.Contain("-150.00"));

        // FiatDetails - Income (positive fiat)
        Assert.That(template, Does.Contain("Salary"));
        Assert.That(template, Does.Contain("5000.00"));

        // FiatToBitcoinDetails - Buy bitcoin
        Assert.That(template, Does.Contain("Stack sats"));
        Assert.That(template, Does.Contain("[btc]"));

        // BitcoinToFiatDetails - Sell bitcoin
        Assert.That(template, Does.Contain("Sold bitcoin"));

        // FiatToFiatDetails - Transfer between fiat
        Assert.That(template, Does.Contain("Transfer to savings"));

        // BitcoinToBitcoinDetails - Transfer between bitcoin
        Assert.That(template, Does.Contain("Consolidate bitcoin"));

        // BitcoinDetails - Bitcoin income
        Assert.That(template, Does.Contain("Bitcoin income"));
    }

    [Test]
    public void Should_Generate_Template_With_Correct_Account_Naming()
    {
        // Act
        var template = _generator.GenerateTemplate();

        // Assert: Fiat accounts should have currency codes
        Assert.That(template, Does.Contain("[USD]"));
        Assert.That(template, Does.Contain("[BRL]"));
        Assert.That(template, Does.Contain("[EUR]"));

        // Assert: Bitcoin accounts should have [btc] suffix
        Assert.That(template, Does.Contain("[btc]"));
    }

    [Test]
    public void Should_Generate_Template_With_Multiple_Rows()
    {
        // Act
        var template = _generator.GenerateTemplate();
        var lines = template.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        // Assert: Header + 7 sample rows
        Assert.That(lines.Length, Is.GreaterThanOrEqualTo(7));
    }

    #endregion

    #region Round-Trip Tests

    [Test]
    public void Template_Should_Be_Parseable_By_CsvImportParser()
    {
        // Arrange
        var template = _generator.GenerateTemplate();

        // Act
        var result = _parser.Parse(template);

        // Assert
        Assert.That(result.IsSuccess, Is.True,
            $"Template failed to parse. Errors: {string.Join(", ", result.Errors)}");
        Assert.That(result.Rows.Count, Is.GreaterThanOrEqualTo(6),
            "Template should have at least 6 sample rows (one per transaction type)");
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Template_Rows_Should_Have_Valid_Data()
    {
        // Arrange
        var template = _generator.GenerateTemplate();

        // Act
        var result = _parser.Parse(template);

        // Assert
        foreach (var row in result.Rows)
        {
            Assert.That(row.Date, Is.GreaterThan(DateOnly.MinValue), "Each row should have a valid date");
            Assert.That(row.Description, Is.Not.Empty, "Each row should have a description");
            Assert.That(row.Amount, Is.Not.EqualTo(0m), "Each row should have a non-zero amount");
            Assert.That(row.AccountName, Is.Not.Empty, "Each row should have an account name");
            Assert.That(row.CategoryName, Is.Not.Empty, "Each row should have a category name");
        }
    }

    [Test]
    public void Template_Should_Include_Transfer_Examples()
    {
        // Arrange
        var template = _generator.GenerateTemplate();

        // Act
        var result = _parser.Parse(template);

        // Assert: At least some rows should have to_account (for transfers/exchanges)
        var rowsWithToAccount = result.Rows.Where(r => r.ToAccountName != null).ToList();
        Assert.That(rowsWithToAccount.Count, Is.GreaterThanOrEqualTo(4),
            "Template should include at least 4 transfer/exchange examples");
    }

    [Test]
    public void Template_Should_Include_Bitcoin_Amount_Examples()
    {
        // Arrange
        var template = _generator.GenerateTemplate();

        // Act
        var result = _parser.Parse(template);

        // Assert: Some rows should have small decimal amounts typical for bitcoin
        var rowsWithSmallAmounts = result.Rows.Where(r =>
            Math.Abs(r.Amount) < 1m || (r.ToAmount.HasValue && r.ToAmount.Value < 1m)).ToList();
        Assert.That(rowsWithSmallAmounts.Count, Is.GreaterThanOrEqualTo(1),
            "Template should include bitcoin amount examples (values less than 1)");
    }

    #endregion

    #region File Save Tests

    [Test]
    public void Should_Save_Template_To_File()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"valt_template_{Guid.NewGuid()}.csv");

        try
        {
            // Act
            _generator.SaveTemplate(tempPath);

            // Assert
            Assert.That(File.Exists(tempPath), Is.True);
            var content = File.ReadAllText(tempPath);
            Assert.That(content, Does.StartWith("date,description,amount"));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    #endregion
}
