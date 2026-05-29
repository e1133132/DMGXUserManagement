using System.Diagnostics;
using System.Net.Http.Json;
using UserManagement.Core.DTOs;
using UserManagement.StressTests.Reporting;

namespace UserManagement.StressTests;

public class ApiStressTests(StressTestHost host) : IClassFixture<StressTestHost>
{
    private readonly HttpClient _client = host.Factory.CreateClient();
    private readonly StressReportCollector _reports = host.Reports;

    [Fact]
    public async Task ConcurrentCreateAndRead_MeetsThroughputThreshold()
    {
        const int parallelRequests = 50;
        var tasks = new List<Task<HttpResponseMessage>>(parallelRequests);

        for (var i = 0; i < parallelRequests; i++)
        {
            var email = $"stress{i}@load.test";
            tasks.Add(_client.PostAsJsonAsync("/api/users", new CreateUserRequest(email, $"User {i}")));
        }

        var sw = Stopwatch.StartNew();
        var responses = await Task.WhenAll(tasks);
        sw.Stop();

        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var successRate = successCount * 100.0 / parallelRequests;

        try
        {
            Assert.All(responses, r => Assert.True(
                r.IsSuccessStatusCode || r.StatusCode == System.Net.HttpStatusCode.Conflict,
                $"Unexpected status: {r.StatusCode}"));

            Assert.True(successCount >= parallelRequests * 0.9,
                $"Expected at least 90% success, got {successCount}/{parallelRequests}");

            Assert.True(sw.ElapsedMilliseconds < 30_000,
                $"Stress run took too long: {sw.ElapsedMilliseconds}ms");

            _reports.Add(new StressTestResult(
                nameof(ConcurrentCreateAndRead_MeetsThroughputThreshold),
                Passed: true,
                sw.ElapsedMilliseconds,
                parallelRequests,
                successCount,
                successRate));
        }
        catch (Exception ex)
        {
            _reports.Add(new StressTestResult(
                nameof(ConcurrentCreateAndRead_MeetsThroughputThreshold),
                Passed: false,
                sw.ElapsedMilliseconds,
                parallelRequests,
                successCount,
                successRate,
                ex.Message));
            throw;
        }
    }

    [Fact]
    public async Task HealthEndpoint_HandlesBurstLoad()
    {
        const int burst = 200;
        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, burst)
            .Select(_ => _client.GetAsync("/health"))
            .ToArray();
        var responses = await Task.WhenAll(tasks);
        sw.Stop();

        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var successRate = successCount * 100.0 / burst;

        try
        {
            Assert.All(responses, r => r.EnsureSuccessStatusCode());
            Assert.True(sw.ElapsedMilliseconds < 15_000,
                $"Health burst took {sw.ElapsedMilliseconds}ms");

            _reports.Add(new StressTestResult(
                nameof(HealthEndpoint_HandlesBurstLoad),
                Passed: true,
                sw.ElapsedMilliseconds,
                burst,
                successCount,
                successRate));
        }
        catch (Exception ex)
        {
            _reports.Add(new StressTestResult(
                nameof(HealthEndpoint_HandlesBurstLoad),
                Passed: false,
                sw.ElapsedMilliseconds,
                burst,
                successCount,
                successRate,
                ex.Message));
            throw;
        }
    }
}
