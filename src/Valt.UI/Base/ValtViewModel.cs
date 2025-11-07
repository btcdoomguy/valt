using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.UI.Views;

namespace Valt.UI.Base;

public abstract class ValtViewModel : ObservableObject
{
    public MainViewTabNames PageName { get; init; }
    public Func<Window?> GetUserControlOwnerWindow { get; set; } = null!;
}