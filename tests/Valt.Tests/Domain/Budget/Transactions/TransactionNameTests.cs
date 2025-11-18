using Valt.Core.Modules.Budget.Transactions;

namespace Valt.Tests.Domain.Budget.Transactions;

[TestFixture]
public class TransactionNameTests
{
    [Test]
    public void Should_be_equal()
    {
        var instance1 = TransactionName.New("Test");
        var instance2 = TransactionName.New("Test");

        Assert.That(instance2, Is.EqualTo(instance1));
        Assert.That(instance1 == instance2);
    }
}