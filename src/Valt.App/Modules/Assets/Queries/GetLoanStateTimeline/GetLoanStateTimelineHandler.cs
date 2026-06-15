using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;

namespace Valt.App.Modules.Assets.Queries.GetLoanStateTimeline;

internal sealed class GetLoanStateTimelineHandler : IQueryHandler<GetLoanStateTimelineQuery, IReadOnlyList<LoanStateSnapshotDTO>>
{
    private readonly IAssetRepository _assetRepository;

    public GetLoanStateTimelineHandler(IAssetRepository assetRepository)
    {
        _assetRepository = assetRepository;
    }

    public async Task<IReadOnlyList<LoanStateSnapshotDTO>> HandleAsync(GetLoanStateTimelineQuery query, CancellationToken ct = default)
    {
        var asset = await _assetRepository.GetByIdAsync(new AssetId(query.AssetId));
        if (asset is null || asset.Details is not BtcLoanDetails btcLoan)
            return new List<LoanStateSnapshotDTO>().AsReadOnly();

        return btcLoan.Snapshots
            .OrderBy(s => s.EffectiveDate)
            .Select(MapSnapshot)
            .ToList()
            .AsReadOnly();
    }

    private static LoanStateSnapshotDTO MapSnapshot(LoanStateSnapshot snapshot)
    {
        return new LoanStateSnapshotDTO
        {
            PlatformName = snapshot.PlatformName,
            CollateralSats = snapshot.CollateralSats,
            LoanAmount = snapshot.LoanAmount,
            CurrencyCode = snapshot.CurrencyCode,
            Apr = snapshot.Apr,
            InitialLtv = snapshot.InitialLtv,
            LiquidationLtv = snapshot.LiquidationLtv,
            MarginCallLtv = snapshot.MarginCallLtv,
            Fees = snapshot.Fees,
            LoanStartDate = snapshot.LoanStartDate,
            RepaymentDate = snapshot.RepaymentDate,
            StatusId = (int)snapshot.Status,
            CurrentBtcPriceInLoanCurrency = snapshot.CurrentBtcPriceInLoanCurrency,
            FixedTotalDebt = snapshot.FixedTotalDebt,
            CurrentTotalDebt = snapshot.CurrentTotalDebt,
            EffectiveDate = snapshot.EffectiveDate,
            Note = snapshot.Note
        };
    }
}
