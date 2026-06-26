using System.Text.RegularExpressions;

namespace Valt.Tests.Architecture;

[TestFixture]
public class BlockingCallTests
{
    private static readonly Regex BlockingAwaitPattern = new(
        @"GetAwaiter\s*\(\s*\)\.\s*GetResult\s*\(\s*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Test]
    public void UiViewModels_Should_Not_Contain_GetAwaiterGetResult_BlockingPattern()
    {
        var repositoryRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        var uiProjectPath = Path.Combine(repositoryRoot, "src", "Valt.UI");

        var csFiles = Directory.EnumerateFiles(uiProjectPath, "*.cs", SearchOption.AllDirectories);

        var violations = new List<string>();
        foreach (var file in csFiles)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (BlockingAwaitPattern.IsMatch(lines[i]))
                {
                    var relativePath = Path.GetRelativePath(repositoryRoot, file);
                    violations.Add($"{relativePath}:{i + 1}");
                }
            }
        }

        Assert.That(violations, Is.Empty,
            $"Found .GetAwaiter().GetResult() blocking pattern in the following UI files:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }
}
