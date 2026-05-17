using System.Diagnostics;
using System.Net.Http.Json;
using UserManagement.Core.DTOs;

namespace UserManagement.StressTests;

public class ApiStressTests : IClassFixture<StressWebAppFactory>
{
    private readonly HttpClient _client;

    public ApiStressTests(StressWebAppFactory factory) => _client = factory.CreateClient();

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

        Assert.All(responses, r => Assert.True(
            r.IsSuccessStatusCode || r.StatusCode == System.Net.HttpStatusCode.Conflict,
            $"Unexpected status: {r.StatusCode}"));

        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        Assert.True(successCount >= parallelRequests * 0.9,
            $"Expected at least 90% success, got {successCount}/{parallelRequests}");

        Assert.True(sw.ElapsedMilliseconds < 30_000,
            $"Stress run took too long: {sw.ElapsedMilliseconds}ms");
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

        Assert.All(responses, r => r.EnsureSuccessStatusCode());
        Assert.True(sw.ElapsedMilliseconds < 15_000,
            $"Health burst took {sw.ElapsedMilliseconds}ms");
    }
}
