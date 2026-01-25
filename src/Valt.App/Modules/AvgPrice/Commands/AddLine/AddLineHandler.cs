using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Common;
using Valt.Core.Modules.AvgPrice;
using Valt.Core.Modules.AvgPrice.Calculations;

namespace Valt.App.Modules.AvgPrice.Commands.AddLine;

internal sealed class AddLineHandler : ICommandHandler<AddLineCommand, AddLineResult>
{
    private readonly IAvgPriceRepository _avgPriceRepository;
    private readonly IValidator<AddLineCommand> _validator;

    public AddLineHandler(
        IAvgPriceRepository avgPriceRepository,
        IValidator<AddLineCommand> validator)
    {
        _avgPriceRepository = avgPriceRepository;
        _validator = validator;
    }

    public async Task<Result<AddLineResult>> HandleAsync(
        AddLineCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<AddLineResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        var profile = await _avgPriceRepository.GetAvgPriceProfileByIdAsync(
            new AvgPriceProfileId(command.ProfileId));

        if (profile is null)
            return Result<AddLineResult>.Failure(
                "PROFILE_NOT_FOUND", $"Profile with id {command.ProfileId} not found");

        var lineType = (AvgPriceLineTypes)command.LineTypeId;
        var amount = FiatValue.New(command.Amount);

        // Get the next display order (AddLine internally calculates this)
        var displayOrder = 0;

        profile.AddLine(
            command.Date,
            displayOrder,
            lineType,
            command.Quantity,
            amount,
            command.Comment ?? string.Empty);

        await _avgPriceRepository.SaveAvgPriceProfileAsync(profile);

        // The new line is the last one added
        var newLine = profile.AvgPriceLines.Last();

        return Result<AddLineResult>.Success(new AddLineResult(newLine.Id.Value));
    }
}
