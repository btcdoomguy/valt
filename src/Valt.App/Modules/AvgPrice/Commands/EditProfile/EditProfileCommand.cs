using Valt.App.Kernel.Commands;

namespace Valt.App.Modules.AvgPrice.Commands.EditProfile;

public record EditProfileCommand : ICommand<EditProfileResult>
{
    public required string ProfileId { get; init; }
    public required string Name { get; init; }
    public required string AssetName { get; init; }
    public required int Precision { get; init; }
    public required bool Visible { get; init; }
    public string? IconName { get; init; }
    public char IconUnicode { get; init; }
    public int IconColor { get; init; }

    /// <summary>
    /// Calculation method: 0=BrazilianRule, 1=Fifo
    /// </summary>
    public required int CalculationMethodId { get; init; }
}

public record EditProfileResult;
