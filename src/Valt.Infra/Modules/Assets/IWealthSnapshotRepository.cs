namespace Valt.Infra.Modules.Assets;

public interface IWealthSnapshotRepository
{
    Task<WealthSnapshotEntity?> GetByDayAsync(DateOnly day);
    Task SaveAsync(WealthSnapshotEntity entity);
    Task<IReadOnlyList<WealthSnapshotEntity>> GetAllAsync();
}
