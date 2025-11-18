using System;

namespace Valt.UI.Views.Main.Modals.TransactionEditor.Exceptions;

public class MissingToAccountException : Exception
{
    public MissingToAccountException() : base("Destination account is required")
    {
    }
}