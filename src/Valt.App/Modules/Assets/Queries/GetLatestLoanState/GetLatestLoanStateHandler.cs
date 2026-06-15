using Valt.App.Kernel.Queries;
using Valt.App.Modules.Assets.DTOs;
using Valt.Core.Modules.Assets;
using Valt.Core.Modules.Assets.Contracts;
using Valt.Core.Modules.Assets.Details;

namespace Valt.App.Modules.Assets.Queries.GetLatestLoanState;

internal sealed class GetLatestLoanStateHandler : IQueryHandler<GetLatestLoanStateQuery, LoanStateDTO?>
{
    private readonly IAssetRepository _assetRepository;

    public GetLatestLoanStateHandler(IAssetRepository assetRepository)
    {
        _assetRepository = assetRepository;
    }

    public async Task<LoanStateDTO?> HandleAsync(GetLatestLoanStateQuery query, CancellationToken ct = default)
    {
        var asset = await _assetRepository.GetByIdAsync(new AssetId(query.AssetId));
        if (asset is null || asset.Details is not BtcLoanDetails btcLoan)
            return null;

        var latestSnapshot = btcLoan.Snapshots.MaxBy(s => s.EffectiveDate);
        if (latestSnapshot is null)
            return null;

        return new LoanStateDTO
        {
            AssetId = asset.Id.Value,
            AssetName = asset.Name.Value,
            PlatformName = latestSnapshot.PlatformName,
            CollateralSats = latestSnapshot.CollateralSats,
            LoanAmount = latestSnapshot.LoanAmount,
            CurrencyCode = latestSnapshot.CurrencyCode,
            Apr = latestSnapshot.Apr,
            InitialLtv = latestSnapshot.InitialLtv,
            LiquidationLtv = latestSnapshot.LiquidationLtv,
            MarginCallLtv = latestSnapshot.MarginCallLtv,
            Fees = latestSnapshot.Fees,
            LoanStartDate = latestSnapshot.LoanStartDate,
            RepaymentDate = latestSnapshot.RepaymentDate,
            StatusId = (int)latestSnapshot.Status,
            CurrentBtcPriceInLoanCurrency = latestSnapshot.CurrentBtcPriceInLoanCurrency,
            FixedTotalDebt = latestSnapshot.FixedTotalDebt,
            CurrentTotalDebt = latestSnapshot.CurrentTotalDebt,
            EffectiveDate = latestSnapshot.EffectiveDate,
            Note = latestSnapshot.Note
        };
    }
}
