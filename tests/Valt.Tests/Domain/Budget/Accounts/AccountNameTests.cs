using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Exceptions;

namespace Valt.Tests.Domain.Budget.Accounts;

[TestFixture]
public class AccountNameTests
{
    [Test]
    public void Should_Throw_Error_If_Empty()
    {
        Assert.Throws<EmptyAccountNameException>(() => AccountName.New(""));
    }

    [Test]
    public void Should_Throw_Error_If_Name_Bigger_Than_30_Chars()
    {
        Assert.Throws<MaximumFieldLengthException>(() => AccountName.New("1234567890123456789012345678901"));
    }
}