using System.Text;
using System.Text.Json;

namespace UserManagement.StressTests.Reporting;

public class StressReportCollector : IDisposable
{
    private readonly List<StressTestResult> _results = [];
    private readonly object _lock = new();

    public void Add(StressTestResult result)
    {
        lock (_lock)
        {
            _results.Add(result);
            WriteReports();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            WriteReports();
        }
    }

    private void WriteReports()
    {
        List<StressTestResult> snapshot;
        lock (_lock)
        {
            snapshot = _results.ToList();
        }

        var outputDir = ResolveOutputDirectory();
        Directory.CreateDirectory(outputDir);

        var jsonPath = Path.Combine(outputDir, "stress-performance-report.json");
        var htmlPath = Path.Combine(outputDir, "stress-performance-report.html");

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonPath, json, Encoding.UTF8);
        File.WriteAllText(htmlPath, BuildHtml(snapshot), Encoding.UTF8);
    }

    private static string BuildHtml(IReadOnlyList<StressTestResult> results)
    {
        var rows = string.Join(
            "\n",
            results.Select(r => $"""
                <tr class="{(r.Passed ? "pass" : "fail")}">
                  <td>{Escape(r.TestName)}</td>
                  <td>{(r.Passed ? "PASS" : "FAIL")}</td>
                  <td>{r.ElapsedMilliseconds:N0} ms</td>
                  <td>{r.SuccessfulRequests} / {r.TotalRequests}</td>
                  <td>{r.SuccessRatePercent:F1}%</td>
                  <td>{Escape(r.Message ?? "-")}</td>
                </tr>
                """));

        var generatedAt = DateTime.UtcNow.ToString("u");
        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <title>Stress Test Performance Report</title>
              <style>
                body { font-family: Segoe UI, sans-serif; margin: 2rem; color: #1a1a1a; }
                h1 { margin-bottom: 0.25rem; }
                .meta { color: #555; margin-bottom: 1.5rem; }
                table { border-collapse: collapse; width: 100%; }
                th, td { border: 1px solid #ddd; padding: 0.6rem 0.8rem; text-align: left; }
                th { background: #f5f5f5; }
                tr.pass td:nth-child(2) { color: #0a7a2f; font-weight: 600; }
                tr.fail td:nth-child(2) { color: #b00020; font-weight: 600; }
              </style>
            </head>
            <body>
              <h1>Stress Test Performance Report</h1>
              <p class="meta">Generated at {{generatedAt}} (UTC)</p>
              <table>
                <thead>
                  <tr>
                    <th>Test</th>
                    <th>Status</th>
                    <th>Duration</th>
                    <th>Requests</th>
                    <th>Success Rate</th>
                    <th>Notes</th>
                  </tr>
                </thead>
                <tbody>
            {{rows}}
                </tbody>
              </table>
            </body>
            </html>
            """;
    }

    private static string Escape(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string ResolveOutputDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("TEST_RESULTS_DIR");
        var repoRoot = FindRepoRoot();

        if (string.IsNullOrWhiteSpace(configured))
        {
            return Path.Combine(repoRoot, "TestResults", "stress");
        }

        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(repoRoot, configured);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "UserManagement.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }

        return Directory.GetCurrentDirectory();
    }
}
