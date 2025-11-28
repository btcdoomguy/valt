using System;
using Valt.UI.Base;
using Valt.UI.Views;

namespace Valt.UI.Services;

public class PageFactory : IPageFactory
{
    private readonly Func<MainViewTabNames, ValtTabViewModel> _factoryMethod;

    public PageFactory(Func<MainViewTabNames, ValtTabViewModel> factoryMethod)
    {
        _factoryMethod = factoryMethod;
    }

    public ValtTabViewModel Create(MainViewTabNames pageName) => _factoryMethod.Invoke(pageName);
}