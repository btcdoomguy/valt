using Valt.Infra.Services.CsvImport;

namespace Valt.Tests.CsvImport;

/// <summary>
/// Tests for the CsvImportParser service.
/// Verifies CSV parsing with various valid and invalid inputs.
/// </summary>
[TestFixture]
public class CsvImportParserTests
{
    private ICsvImportParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new CsvImportParser();
    }

    #region Happy Path Tests

    [Test]
    public void Should_Parse_Valid_Csv_With_All_Fields()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,Grocery shopping,-150.00,Checking [USD],Savings [USD],150.00,Food
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Rows, Has.Count.EqualTo(1));
        Assert.That(result.Errors, Is.Empty);

        var row = result.Rows[0];
        Assert.That(row.Date, Is.EqualTo(new DateOnly(2024, 1, 15)));
        Assert.That(row.Description, Is.EqualTo("Grocery shopping"));
        Assert.That(row.Amount, Is.EqualTo(-150.00m));
        Assert.That(row.AccountName, Is.EqualTo("Checking [USD]"));
        Assert.That(row.ToAccountName, Is.EqualTo("Savings [USD]"));
        Assert.That(row.ToAmount, Is.EqualTo(150.00m));
        Assert.That(row.CategoryName, Is.EqualTo("Food"));
        Assert.That(row.LineNumber, Is.EqualTo(2));
    }

    [Test]
    public void Should_Parse_Csv_With_Optional_Fields_Empty()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,Salary,5000.00,Checking [USD],,,Income
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Rows, Has.Count.EqualTo(1));

        var row = result.Rows[0];
        Assert.That(row.ToAccountName, Is.Null);
        Assert.That(row.ToAmount, Is.Null);
    }

    [Test]
    public void Should_Handle_Multiple_Rows()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,Expense 1,-100.00,Account 1,,,Food
            2024-01-16,Expense 2,-200.00,Account 2,,,Transport
            2024-01-17,Income 1,3000.00,Account 3,,,Income
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Rows, Has.Count.EqualTo(3));
        Assert.That(result.Rows[0].Description, Is.EqualTo("Expense 1"));
        Assert.That(result.Rows[1].Description, Is.EqualTo("Expense 2"));
        Assert.That(result.Rows[2].Description, Is.EqualTo("Income 1"));
    }

    [Test]
    public void Should_Parse_Bitcoin_Amounts_With_8_Decimals()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,Buy BTC,-500.00,Checking [USD],Wallet [btc],0.00850000,Investment
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Rows[0].ToAmount, Is.EqualTo(0.00850000m));
    }

    [Test]
    public void Should_Parse_From_Stream()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,Test,-100.00,Account,,,Category
            """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        // Act
        var result = _parser.Parse(stream);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Rows, Has.Count.EqualTo(1));
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public void Should_Return_Error_For_Missing_Required_Field_Date()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            ,Expense,-100.00,Account,,,Food
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0], Does.Contain("Line 2"));
        Assert.That(result.Errors[0], Does.Contain("date"));
    }

    [Test]
    public void Should_Return_Error_For_Missing_Required_Field_Description()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,,-100.00,Account,,,Food
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("description"));
    }

    [Test]
    public void Should_Return_Error_For_Missing_Required_Field_Amount()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,Expense,,Account,,,Food
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("amount"));
    }

    [Test]
    public void Should_Return_Error_For_Missing_Required_Field_Account()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,Expense,-100.00,,,,Food
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("account"));
    }

    [Test]
    public void Should_Return_Error_For_Missing_Required_Field_Category()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,Expense,-100.00,Account,,,
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("category"));
    }

    [Test]
    public void Should_Return_Error_For_Invalid_Date_Format()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            15/01/2024,Expense,-100.00,Account,,,Food
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("Invalid date format"));
        Assert.That(result.Errors[0], Does.Contain("yyyy-MM-dd"));
    }

    [Test]
    public void Should_Return_Error_For_Invalid_Amount_Format()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,Expense,abc,Account,,,Food
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("Invalid amount format"));
    }

    [Test]
    public void Should_Return_Error_For_Invalid_ToAmount_Format()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,Transfer,-100.00,Account1,Account2,invalid,Transfer
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("to_amount"));
    }

    [Test]
    public void Should_Include_Line_Number_In_Error_Messages()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,Valid,-100.00,Account,,,Food
            2024-01-16,Valid,-200.00,Account,,,Food
            invalid-date,Invalid,-300.00,Account,,,Food
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.Rows, Has.Count.EqualTo(2));
        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0], Does.Contain("Line 4"));
    }

    [Test]
    public void Should_Return_Partial_Success_With_Valid_And_Invalid_Rows()
    {
        // Arrange
        var csv = """
            date,description,amount,account,to_account,to_amount,category
            2024-01-15,Valid,-100.00,Account,,,Food
            invalid-date,Invalid,-200.00,Account,,,Food
            2024-01-17,Also Valid,-300.00,Account,,,Food
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.True); // Partial success since some rows are valid
        Assert.That(result.Rows, Has.Count.EqualTo(2));
        Assert.That(result.Errors, Has.Count.EqualTo(1));
    }

    [Test]
    public void Should_Return_Error_For_Missing_Headers()
    {
        // Arrange
        var csv = """
            date,description,amount
            2024-01-15,Expense,-100.00
            """;

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("Missing required columns"));
        Assert.That(result.Errors[0], Does.Contain("account"));
        Assert.That(result.Errors[0], Does.Contain("category"));
    }

    [Test]
    public void Should_Return_Error_For_Empty_Csv()
    {
        // Arrange
        var csv = "";

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(1));
    }

    [Test]
    public void Should_Return_Error_For_Header_Only_Csv()
    {
        // Arrange
        var csv = "date,description,amount,account,to_account,to_amount,category";

        // Act
        var result = _parser.Parse(csv);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors[0], Does.Contain("no data rows"));
    }

    #endregion
}
