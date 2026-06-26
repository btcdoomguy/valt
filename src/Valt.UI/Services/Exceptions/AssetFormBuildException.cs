using System;

namespace Valt.UI.Services.Exceptions;

/// <summary>
/// Exception thrown when the asset form builder encounters an unsupported asset type or invalid form state.
/// </summary>
public class AssetFormBuildException : Exception
{
    public AssetFormBuildException() : base("Error while building asset form.")
    {
    }

    public AssetFormBuildException(string message) : base(message)
    {
    }
}
