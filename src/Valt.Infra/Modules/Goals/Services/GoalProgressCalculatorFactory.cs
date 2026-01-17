using Valt.Core.Modules.Goals;

namespace Valt.Infra.Modules.Goals.Services;

internal class GoalProgressCalculatorFactory : IGoalProgressCalculatorFactory
{
    private readonly IEnumerable<IGoalProgressCalculator> _calculators;

    public GoalProgressCalculatorFactory(IEnumerable<IGoalProgressCalculator> calculators)
    {
        _calculators = calculators;
    }

    public IGoalProgressCalculator GetCalculator(GoalTypeNames typeName)
    {
        return _calculators.FirstOrDefault(c => c.SupportedType == typeName)
               ?? throw new NotSupportedException($"No calculator registered for goal type: {typeName}");
    }
}
