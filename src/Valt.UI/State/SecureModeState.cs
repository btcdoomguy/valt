using System;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Valt.UI.State.Events;

namespace Valt.UI.State;

public partial class SecureModeState : ObservableObject
{
    private string? _passwordHash;

    [ObservableProperty]
    private bool _isEnabled = false;

    partial void OnIsEnabledChanged(bool value)
    {
        WeakReferenceMessenger.Default.Send(new SecureModeChanged(value));
    }

    /// <summary>
    /// Stores the password hash for later verification when leaving secure mode.
    /// </summary>
    public void SetPassword(string password)
    {
        _passwordHash = HashPassword(password);
    }

    /// <summary>
    /// Verifies if the provided password matches the stored hash.
    /// </summary>
    public bool VerifyPassword(string password)
    {
        if (string.IsNullOrEmpty(_passwordHash))
            return true; // No password set, allow

        return _passwordHash == HashPassword(password);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public void Reset()
    {
        _passwordHash = null;
        IsEnabled = false;
    }
}
