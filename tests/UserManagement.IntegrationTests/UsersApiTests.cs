using System.Net;
using System.Net.Http.Json;
using UserManagement.Core.DTOs;

namespace UserManagement.IntegrationTests;

public class UsersApiTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task FullUserLifecycle_Works()
    {
        var create = await _client.PostAsJsonAsync("/api/users", new CreateUserRequest("demo@test.com", "Demo User"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(created);

        var get = await _client.GetAsync($"/api/users/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var update = await _client.PutAsJsonAsync(
            $"/api/users/{created.Id}",
            new UpdateUserRequest("demo@test.com", "Updated Name"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var lockResp = await _client.PostAsync($"/api/users/{created.Id}/lock", null);
        Assert.Equal(HttpStatusCode.OK, lockResp.StatusCode);
        var locked = await lockResp.Content.ReadFromJsonAsync<UserResponse>();
        Assert.True(locked!.IsLocked);

        var updateWhileLocked = await _client.PutAsJsonAsync(
            $"/api/users/{created.Id}",
            new UpdateUserRequest("demo@test.com", "Should Fail"));
        Assert.Equal(HttpStatusCode.Conflict, updateWhileLocked.StatusCode);

        var unlockResp = await _client.PostAsync($"/api/users/{created.Id}/unlock", null);
        Assert.Equal(HttpStatusCode.OK, unlockResp.StatusCode);

        var delete = await _client.DeleteAsync($"/api/users/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var notFound = await _client.GetAsync($"/api/users/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
