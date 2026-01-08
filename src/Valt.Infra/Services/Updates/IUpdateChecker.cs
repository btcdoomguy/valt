namespace Valt.Infra.Services.Updates;

public interface IUpdateChecker
{
    Task<UpdateInfo?> CheckForUpdateAsync(Version currentVersion);
}
