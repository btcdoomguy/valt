using Valt.UI.Base;
using Valt.UI.Views;

namespace Valt.UI.Services;

public interface IPageFactory
{
    ValtViewModel Create(MainViewTabNames pageName);
}