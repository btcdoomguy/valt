using Valt.Core.Modules.Budget.Transactions;

namespace Valt.Tests.Domain.Budget.Transactions;

/// <summary>
/// Tests for the TransactionName value object.
/// TransactionName represents the name/description of a transaction.
/// </summary>
[TestFixture]
public class TransactionNameTests
{
    #region Equality Tests

    [Test]
    public void Should_Be_Equal_When_Values_Are_Same()
    {
        // Arrange
        var instance1 = TransactionName.New("Test");
        var instance2 = TransactionName.New("Test");

        // Assert: Two TransactionNames with same value should be equal
        Assert.That(instance2, Is.EqualTo(instance1));
        Assert.That(instance1 == instance2, Is.True);
    }

    #endregion
}
