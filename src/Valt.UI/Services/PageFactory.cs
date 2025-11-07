using System;
using Valt.UI.Base;
using Valt.UI.Views;

namespace Valt.UI.Services;

public class PageFactory : IPageFactory
{
    private readonly Func<MainViewTabNames, ValtViewModel> _factoryMethod;

    public PageFactory(Func<MainViewTabNames, ValtViewModel> factoryMethod)
    {
        _factoryMethod = factoryMethod;
    }

    public ValtViewModel Create(MainViewTabNames pageName) => _factoryMethod.Invoke(pageName);
}