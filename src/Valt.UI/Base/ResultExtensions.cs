using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Valt.App.Kernel;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Base;

/// <summary>
/// Extension methods for handling Result types in ViewModels.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Handles a Result by executing success/error callbacks and optionally displaying errors.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="resultTask">The result task to handle.</param>
    /// <param name="getWindow">Function to get the owner window for error dialogs.</param>
    /// <param name="onSuccess">Callback executed on success.</param>
    /// <param name="onError">Custom error handler. Return true to suppress default error display.</param>
    /// <returns>The success value if successful, default otherwise.</returns>
    public static async Task<T?> HandleAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Window?>? getWindow = null,
        Func<T, Task>? onSuccess = null,
        Func<Error, Task<bool>>? onError = null)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            if (onSuccess != null)
                await onSuccess(result.Value!);
            return result.Value;
        }

        // Custom error handling
        if (onError != null && await onError(result.Error!))
            return default;

        // Default error display
        var window = getWindow?.Invoke();
        if (window != null)
            await ShowErrorAsync(result.Error!, window);

        return default;
    }

    /// <summary>
    /// Handles a Result by executing success/error callbacks and optionally displaying errors.
    /// Returns true if successful.
    /// </summary>
    public static async Task<bool> HandleSuccessAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Window?>? getWindow = null,
        Func<T, Task>? onSuccess = null,
        Func<Error, Task<bool>>? onError = null)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            if (onSuccess != null)
                await onSuccess(result.Value!);
            return true;
        }

        // Custom error handling
        if (onError != null && await onError(result.Error!))
            return false;

        // Default error display
        var window = getWindow?.Invoke();
        if (window != null)
            await ShowErrorAsync(result.Error!, window);

        return false;
    }

    /// <summary>
    /// Displays an error to the user using the appropriate message box.
    /// </summary>
    public static async Task ShowErrorAsync(Error error, Window window)
    {
        if (error.ValidationErrors?.Count > 0)
        {
            var message = string.Join("\n",
                error.ValidationErrors.SelectMany(kv =>
                    kv.Value.Select(v => $"- {v}")));
            await MessageBoxHelper.ShowAlertAsync(
                language.Error_ValidationError, message, window);
        }
        else
        {
            var title = GetErrorTitle(error.Code);
            await MessageBoxHelper.ShowErrorAsync(title, error.Message, window);
        }
    }

    /// <summary>
    /// Gets a localized title for an error code.
    /// </summary>
    private static string GetErrorTitle(string errorCode)
    {
        return errorCode switch
        {
            "VALIDATION_FAILED" => language.Error_ValidationError,
            "TRANSACTION_NOT_FOUND" => language.Error_TransactionNotFound,
            "ACCOUNT_NOT_FOUND" => language.Error_AccountNotFound,
            "GROUP_NOT_FOUND" => language.Error_AccountGroupNotFound,
            "PROFILE_NOT_FOUND" => language.Error_ProfileNotFound,
            "GOAL_NOT_FOUND" => language.Error_GoalNotFound,
            "FIXED_EXPENSE_NOT_FOUND" => language.Error_FixedExpenseNotFound,
            _ => language.Error
        };
    }
}
