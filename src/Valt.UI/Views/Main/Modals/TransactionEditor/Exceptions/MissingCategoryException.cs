using System;

namespace Valt.UI.Views.Main.Modals.TransactionEditor.Exceptions;

public class MissingCategoryException : Exception
{
    public MissingCategoryException() : base("Category is required")
    {
    }
}