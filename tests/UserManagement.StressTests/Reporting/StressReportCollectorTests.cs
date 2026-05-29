namespace UserManagement.StressTests.Reporting;

public class StressReportCollectorTests
{
    [Fact]
    public void WriteReports_CreatesJsonAndHtmlFiles()
    {
        var outputDir = Path.Combine(
            Path.GetTempPath(),
            "stress-smoke-" + Guid.NewGuid().ToString("N"));
        var previousDir = Environment.GetEnvironmentVariable("TEST_RESULTS_DIR");

        try
        {
            Environment.SetEnvironmentVariable("TEST_RESULTS_DIR", outputDir);

            var collector = new StressReportCollector();
            collector.Add(new StressTestResult(
                "Sample",
                Passed: true,
                ElapsedMilliseconds: 120,
                TotalRequests: 50,
                SuccessfulRequests: 48,
                SuccessRatePercent: 96.0));

            collector.Dispose();

            Assert.True(File.Exists(Path.Combine(outputDir, "stress-performance-report.json")));
            Assert.True(File.Exists(Path.Combine(outputDir, "stress-performance-report.html")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_RESULTS_DIR", previousDir);
        }
    }
}
