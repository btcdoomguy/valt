using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Details;

namespace Valt.Tests.Domain.Assets.Details;

[TestFixture]
public class BtcLendingDetailsTests
{
    private static BtcLendingDetails CreateDefaultDetails(
        decimal amountLent = 10_000m,
        string currencyCode = "USD",
        decimal apr = 0.05m,
        DateOnly? expectedRepaymentDate = null,
        string borrowerOrPlatformName = "Ledn",
        DateOnly? lendingStartDate = null,
        LoanStatus status = LoanStatus.Active)
    {
        return new BtcLendingDetails(
            amountLent,
            currencyCode,
            apr,
            expectedRepaymentDate,
            borrowerOrPlatformName,
            lendingStartDate ?? new DateOnly(2025, 1, 1),
            status);
    }

    #region Construction Tests

    [Test]
    public void Should_Create_With_Valid_Parameters()
    {
        var details = CreateDefaultDetails();

        Assert.Multiple(() =>
        {
            Assert.That(details.AssetType, Is.EqualTo(AssetTypes.BtcLending));
            Assert.That(details.AmountLent, Is.EqualTo(10_000m));
            Assert.That(details.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(details.Apr, Is.EqualTo(0.05m));
            Assert.That(details.BorrowerOrPlatformName, Is.EqualTo("Ledn"));
            Assert.That(details.Status, Is.EqualTo(LoanStatus.Active));
        });
    }

    [Test]
    public void Should_Validate_AmountLent_Is_Positive()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateDefaultDetails(amountLent: 0));

        Assert.Throws<ArgumentException>(() =>
            CreateDefaultDetails(amountLent: -1000m));
    }

    [Test]
    public void Should_Validate_Apr_Not_Negative()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateDefaultDetails(apr: -0.01m));
    }

    [Test]
    public void Should_Allow_Zero_Apr()
    {
        var details = CreateDefaultDetails(apr: 0m);
        Assert.That(details.Apr, Is.EqualTo(0m));
    }

    [Test]
    public void Should_Allow_Null_ExpectedRepaymentDate()
    {
        var details = CreateDefaultDetails(expectedRepaymentDate: null);
        Assert.That(details.ExpectedRepaymentDate, Is.Null);
    }

    #endregion

    #region Earned Interest Tests

    [Test]
    public void Should_Calculate_Earned_Interest()
    {
        // $10,000 lent at 5% APR for 365 days
        // Interest = 10000 * 0.05 / 365 * 365 = 500
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);
        var details = CreateDefaultDetails(lendingStartDate: startDate);

        var interest = details.CalculateEarnedInterest();

        Assert.That(interest, Is.EqualTo(500m));
    }

    [Test]
    public void Should_Return_Zero_Interest_When_Just_Started()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var details = CreateDefaultDetails(lendingStartDate: today);

        var interest = details.CalculateEarnedInterest();

        Assert.That(interest, Is.EqualTo(0m));
    }

    [Test]
    public void Should_Return_Zero_Interest_For_Future_Start_Date()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var details = CreateDefaultDetails(lendingStartDate: futureDate);

        var interest = details.CalculateEarnedInterest();

        Assert.That(interest, Is.EqualTo(0m));
    }

    [Test]
    public void Should_Return_Zero_Interest_When_Apr_Is_Zero()
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);
        var details = CreateDefaultDetails(apr: 0m, lendingStartDate: startDate);

        var interest = details.CalculateEarnedInterest();

        Assert.That(interest, Is.EqualTo(0m));
    }

    #endregion

    #region Current Value Tests

    [Test]
    public void Should_Calculate_Current_Value_As_AmountLent_Plus_Interest()
    {
        // Today's start means 0 interest
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var details = CreateDefaultDetails(lendingStartDate: today);

        var value = details.CalculateCurrentValue(0m); // price ignored

        Assert.That(value, Is.EqualTo(10_000m));
    }

    [Test]
    public void Should_Ignore_Price_Parameter()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var details = CreateDefaultDetails(lendingStartDate: today);

        var value1 = details.CalculateCurrentValue(0m);
        var value2 = details.CalculateCurrentValue(100_000m);

        Assert.That(value1, Is.EqualTo(value2));
    }

    #endregion

    #region Days Until Repayment Tests

    [Test]
    public void Should_Calculate_Days_Until_Repayment()
    {
        var repaymentDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60);
        var details = CreateDefaultDetails(expectedRepaymentDate: repaymentDate);

        var days = details.CalculateDaysUntilRepayment();

        Assert.That(days, Is.EqualTo(60));
    }

    [Test]
    public void Should_Return_Null_When_No_Repayment_Date()
    {
        var details = CreateDefaultDetails(expectedRepaymentDate: null);

        var days = details.CalculateDaysUntilRepayment();

        Assert.That(days, Is.Null);
    }

    [Test]
    public void Should_Return_Zero_When_Repayment_Date_Passed()
    {
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        var details = CreateDefaultDetails(expectedRepaymentDate: pastDate);

        var days = details.CalculateDaysUntilRepayment();

        Assert.That(days, Is.EqualTo(0));
    }

    #endregion

    #region WithUpdatedPrice Tests

    [Test]
    public void Should_Return_Self_On_WithUpdatedPrice()
    {
        var details = CreateDefaultDetails();

        var updated = details.WithUpdatedPrice(999m);

        Assert.That(updated, Is.SameAs(details));
    }

    #endregion

    #region WithStatus Tests

    [Test]
    public void Should_Create_New_Details_With_Repaid_Status()
    {
        var original = CreateDefaultDetails();

        var updated = original.WithStatus(LoanStatus.Repaid);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Status, Is.EqualTo(LoanStatus.Repaid));
            Assert.That(updated.AmountLent, Is.EqualTo(original.AmountLent));
            Assert.That(updated.BorrowerOrPlatformName, Is.EqualTo(original.BorrowerOrPlatformName));
            Assert.That(updated.Apr, Is.EqualTo(original.Apr));
        });
    }

    #endregion
}
