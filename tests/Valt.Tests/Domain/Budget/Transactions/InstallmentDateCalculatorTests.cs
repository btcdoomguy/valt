using Valt.Core.Modules.Budget.Transactions.Services;

namespace Valt.Tests.Domain.Budget.Transactions;

[TestFixture]
public class InstallmentDateCalculatorTests
{
    [Test]
    public void Should_Return_Simple_Monthly_Dates()
    {
        // Arrange: Start on the 15th of a month
        var startDate = new DateOnly(2024, 1, 15);

        // Act
        var dates = InstallmentDateCalculator.CalculateInstallmentDates(startDate, 3).ToList();

        // Assert: Should return 15th → 15th → 15th
        Assert.That(dates, Has.Count.EqualTo(3));
        Assert.That(dates[0], Is.EqualTo(new DateOnly(2024, 1, 15)));
        Assert.That(dates[1], Is.EqualTo(new DateOnly(2024, 2, 15)));
        Assert.That(dates[2], Is.EqualTo(new DateOnly(2024, 3, 15)));
    }

    [Test]
    public void Should_Handle_Day_31_With_Shorter_Months()
    {
        // Arrange: Start on January 31
        var startDate = new DateOnly(2024, 1, 31);

        // Act
        var dates = InstallmentDateCalculator.CalculateInstallmentDates(startDate, 4).ToList();

        // Assert: 31 Jan → 29 Feb (leap year) → 31 Mar → 30 Apr
        Assert.That(dates, Has.Count.EqualTo(4));
        Assert.That(dates[0], Is.EqualTo(new DateOnly(2024, 1, 31)));
        Assert.That(dates[1], Is.EqualTo(new DateOnly(2024, 2, 29))); // 2024 is a leap year
        Assert.That(dates[2], Is.EqualTo(new DateOnly(2024, 3, 31)));
        Assert.That(dates[3], Is.EqualTo(new DateOnly(2024, 4, 30)));
    }

    [Test]
    public void Should_Handle_February_In_Non_Leap_Year()
    {
        // Arrange: Start on January 29 in a non-leap year
        var startDate = new DateOnly(2023, 1, 29);

        // Act
        var dates = InstallmentDateCalculator.CalculateInstallmentDates(startDate, 3).ToList();

        // Assert: 29 Jan → 28 Feb (non-leap) → 29 Mar
        Assert.That(dates, Has.Count.EqualTo(3));
        Assert.That(dates[0], Is.EqualTo(new DateOnly(2023, 1, 29)));
        Assert.That(dates[1], Is.EqualTo(new DateOnly(2023, 2, 28)));
        Assert.That(dates[2], Is.EqualTo(new DateOnly(2023, 3, 29)));
    }

    [Test]
    public void Should_Handle_February_In_Leap_Year()
    {
        // Arrange: Start on January 29 in a leap year
        var startDate = new DateOnly(2024, 1, 29);

        // Act
        var dates = InstallmentDateCalculator.CalculateInstallmentDates(startDate, 3).ToList();

        // Assert: 29 Jan → 29 Feb (leap year) → 29 Mar
        Assert.That(dates, Has.Count.EqualTo(3));
        Assert.That(dates[0], Is.EqualTo(new DateOnly(2024, 1, 29)));
        Assert.That(dates[1], Is.EqualTo(new DateOnly(2024, 2, 29)));
        Assert.That(dates[2], Is.EqualTo(new DateOnly(2024, 3, 29)));
    }

    [Test]
    public void Should_Handle_Year_Boundary_Crossing()
    {
        // Arrange: Start in November
        var startDate = new DateOnly(2024, 11, 15);

        // Act
        var dates = InstallmentDateCalculator.CalculateInstallmentDates(startDate, 4).ToList();

        // Assert: Nov → Dec → Jan → Feb
        Assert.That(dates, Has.Count.EqualTo(4));
        Assert.That(dates[0], Is.EqualTo(new DateOnly(2024, 11, 15)));
        Assert.That(dates[1], Is.EqualTo(new DateOnly(2024, 12, 15)));
        Assert.That(dates[2], Is.EqualTo(new DateOnly(2025, 1, 15)));
        Assert.That(dates[3], Is.EqualTo(new DateOnly(2025, 2, 15)));
    }

    [Test]
    public void Should_Return_Single_Date_For_One_Installment()
    {
        // Arrange
        var startDate = new DateOnly(2024, 3, 10);

        // Act
        var dates = InstallmentDateCalculator.CalculateInstallmentDates(startDate, 1).ToList();

        // Assert
        Assert.That(dates, Has.Count.EqualTo(1));
        Assert.That(dates[0], Is.EqualTo(new DateOnly(2024, 3, 10)));
    }

    [Test]
    public void Should_Throw_For_Zero_Installments()
    {
        // Arrange
        var startDate = new DateOnly(2024, 3, 10);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InstallmentDateCalculator.CalculateInstallmentDates(startDate, 0).ToList());
    }

    [Test]
    public void Should_Throw_For_Negative_Installments()
    {
        // Arrange
        var startDate = new DateOnly(2024, 3, 10);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InstallmentDateCalculator.CalculateInstallmentDates(startDate, -1).ToList());
    }

    [Test]
    public void Should_Handle_Many_Installments_Crossing_Multiple_Years()
    {
        // Arrange: 12 installments starting in March
        var startDate = new DateOnly(2024, 3, 31);

        // Act
        var dates = InstallmentDateCalculator.CalculateInstallmentDates(startDate, 12).ToList();

        // Assert
        Assert.That(dates, Has.Count.EqualTo(12));
        Assert.That(dates[0], Is.EqualTo(new DateOnly(2024, 3, 31)));
        Assert.That(dates[1], Is.EqualTo(new DateOnly(2024, 4, 30))); // April has 30 days
        Assert.That(dates[2], Is.EqualTo(new DateOnly(2024, 5, 31)));
        Assert.That(dates[3], Is.EqualTo(new DateOnly(2024, 6, 30))); // June has 30 days
        Assert.That(dates[11], Is.EqualTo(new DateOnly(2025, 2, 28))); // Feb 2025 is not a leap year
    }
}
