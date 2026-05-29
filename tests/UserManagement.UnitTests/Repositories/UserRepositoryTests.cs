using Microsoft.EntityFrameworkCore;
using UserManagement.Core.Entities;
using UserManagement.Infrastructure.Data;
using UserManagement.Infrastructure.Repositories;

namespace UserManagement.UnitTests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly UserRepository _sut;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new UserRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private static User CreateUser(string email, string name = "User") => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        DisplayName = name,
        CreatedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task AddAsync_PersistsUser()
    {
        var user = CreateUser("add@test.com");

        var saved = await _sut.AddAsync(user);

        Assert.Equal(user.Id, saved.Id);
        Assert.Equal(1, await _db.Users.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenExists()
    {
        var user = CreateUser("get@test.com");
        await _sut.AddAsync(user);

        var found = await _sut.GetByIdAsync(user.Id);

        Assert.NotNull(found);
        Assert.Equal("get@test.com", found!.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMissing()
    {
        var found = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenExists()
    {
        var user = CreateUser("email@test.com");
        await _sut.AddAsync(user);

        var found = await _sut.GetByEmailAsync("email@test.com");

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenMissing()
    {
        var found = await _sut.GetByEmailAsync("missing@test.com");

        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsUsersOrderedByEmail()
    {
        await _sut.AddAsync(CreateUser("z@test.com", "Z"));
        await _sut.AddAsync(CreateUser("a@test.com", "A"));

        var all = await _sut.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Equal("a@test.com", all[0].Email);
        Assert.Equal("z@test.com", all[1].Email);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsTrue_WhenUserExists()
    {
        var user = CreateUser("update@test.com");
        await _sut.AddAsync(user);
        user.DisplayName = "Updated";

        var updated = await _sut.UpdateAsync(user);

        Assert.True(updated);
        var reloaded = await _sut.GetByIdAsync(user.Id);
        Assert.Equal("Updated", reloaded!.DisplayName);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenUserExists()
    {
        var user = CreateUser("delete@test.com");
        await _sut.AddAsync(user);

        var deleted = await _sut.DeleteAsync(user.Id);

        Assert.True(deleted);
        Assert.Null(await _sut.GetByIdAsync(user.Id));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenUserMissing()
    {
        var deleted = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }
}
