using System.Text;

namespace Valt.Infra.Kernel;

public static class CrashReportService
{
    public static void Initialize()
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        WriteCrashReport(exception, "UnhandledException");
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteCrashReport(e.Exception, "UnobservedTaskException");
        e.SetObserved();
    }

    public static void WriteCrashReport(Exception? exception, string source)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"crash_{timestamp}.txt";
            var filePath = Path.Combine(ValtEnvironment.AppDataPath, fileName);

            var report = BuildCrashReport(exception, source);
            File.WriteAllText(filePath, report);
        }
        catch
        {
            // If we can't write the crash report, there's nothing more we can do
        }
    }

    private static string BuildCrashReport(Exception? exception, string source)
    {
        var sb = new StringBuilder();

        sb.AppendLine("========================================");
        sb.AppendLine("           VALT CRASH REPORT            ");
        sb.AppendLine("========================================");
        sb.AppendLine();
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Source: {source}");
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($".NET Version: {Environment.Version}");
        sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        sb.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
        sb.AppendLine();

        if (exception is null)
        {
            sb.AppendLine("Exception: (null - no exception information available)");
        }
        else
        {
            AppendExceptionDetails(sb, exception, 0);
        }

        sb.AppendLine();
        sb.AppendLine("========================================");
        sb.AppendLine("           END OF CRASH REPORT          ");
        sb.AppendLine("========================================");

        return sb.ToString();
    }

    private static void AppendExceptionDetails(StringBuilder sb, Exception exception, int level)
    {
        var indent = new string(' ', level * 2);

        if (level > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"{indent}--- Inner Exception (Level {level}) ---");
        }
        else
        {
            sb.AppendLine("--- Exception Details ---");
        }

        sb.AppendLine($"{indent}Type: {exception.GetType().FullName}");
        sb.AppendLine($"{indent}Message: {exception.Message}");

        if (!string.IsNullOrEmpty(exception.Source))
        {
            sb.AppendLine($"{indent}Source: {exception.Source}");
        }

        if (exception.TargetSite != null)
        {
            sb.AppendLine($"{indent}Target Site: {exception.TargetSite}");
        }

        sb.AppendLine();
        sb.AppendLine($"{indent}Stack Trace:");
        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            foreach (var line in exception.StackTrace.Split('\n'))
            {
                sb.AppendLine($"{indent}  {line.Trim()}");
            }
        }
        else
        {
            sb.AppendLine($"{indent}  (no stack trace available)");
        }

        // Handle AggregateException specially
        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                AppendExceptionDetails(sb, innerException, level + 1);
            }
        }
        else if (exception.InnerException != null)
        {
            AppendExceptionDetails(sb, exception.InnerException, level + 1);
        }
    }
}
