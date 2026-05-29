using UserManagement.StressTests.Reporting;

namespace UserManagement.StressTests;

public class StressTestHost : IDisposable
{
    public StressWebAppFactory Factory { get; } = new();
    public StressReportCollector Reports { get; } = new();

    public void Dispose()
    {
        Reports.Dispose();
        Factory.Dispose();
    }
}
