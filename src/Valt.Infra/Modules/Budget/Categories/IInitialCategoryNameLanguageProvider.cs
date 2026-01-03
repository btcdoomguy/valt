namespace Valt.Infra.Modules.Budget.Categories;

public interface IInitialCategoryNameLanguageProvider
{
    string Get(InitialCategoryNames categoryName, string? cultureCode = null);
}