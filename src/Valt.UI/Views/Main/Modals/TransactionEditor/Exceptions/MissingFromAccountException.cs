using System;

namespace Valt.UI.Views.Main.Modals.TransactionEditor.Exceptions;

public class MissingFromAccountException : Exception
{
    public MissingFromAccountException() : base("Origin account is required")
    {
    }
}