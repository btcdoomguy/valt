using Valt.Core.Modules.Goals;

namespace Valt.Infra.Modules.Goals.Services;

public interface IGoalProgressCalculatorFactory
{
    IGoalProgressCalculator GetCalculator(GoalTypeNames typeName);
}
