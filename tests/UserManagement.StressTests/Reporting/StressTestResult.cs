namespace UserManagement.StressTests.Reporting;

public sealed record StressTestResult(
    string TestName,
    bool Passed,
    long ElapsedMilliseconds,
    int TotalRequests,
    int SuccessfulRequests,
    double SuccessRatePercent,
    string? Message = null);
