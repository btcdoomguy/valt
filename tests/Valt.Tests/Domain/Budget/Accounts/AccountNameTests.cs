using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Exceptions;

namespace Valt.Tests.Domain.Budget.Accounts;

/// <summary>
/// Tests for the AccountName value object.
/// AccountName enforces validation rules for account names (non-empty, max 30 chars).
/// </summary>
[TestFixture]
public class AccountNameTests
{
    #region Validation Tests

    [Test]
    public void Should_Throw_Error_If_Empty()
    {
        // Act & Assert: Empty name should throw
        Assert.Throws<EmptyAccountNameException>(() => AccountName.New(""));
    }

    [Test]
    public void Should_Throw_Error_If_Name_Bigger_Than_30_Chars()
    {
        // Arrange: Create a name with 31 characters
        var longName = "1234567890123456789012345678901";

        // Act & Assert: Should throw MaximumFieldLengthException
        Assert.Throws<MaximumFieldLengthException>(() => AccountName.New(longName));
    }

    [Test]
    public void Should_Create_Valid_AccountName()
    {
        // Arrange & Act
        var name = AccountName.New("My Account");

        // Assert
        Assert.That(name.Value, Is.EqualTo("My Account"));
    }

    #endregion
}
