using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Valt.UI.Base;

public abstract class ValtViewModel : ObservableObject
{
    public Func<Window?> GetUserControlOwnerWindow { get; set; } = null!;
}