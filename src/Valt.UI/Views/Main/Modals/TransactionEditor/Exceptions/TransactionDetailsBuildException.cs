using System;

namespace Valt.UI.Views.Main.Modals.TransactionEditor.Exceptions;

public class TransactionDetailsBuildException : Exception
{
    public TransactionDetailsBuildException() : base("Error while building transaction details.")
    {
    }
}