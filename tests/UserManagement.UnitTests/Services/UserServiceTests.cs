using Moq;
using UserManagement.Core.DTOs;
using UserManagement.Core.Entities;
using UserManagement.Core.Interfaces;
using UserManagement.Core.Services;

namespace UserManagement.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly UserService _sut;

    public UserServiceTests() => _sut = new UserService(_repository.Object);

    [Fact]
    public async Task CreateAsync_ReturnsUser_WhenEmailIsUnique()
    {
        _repository.Setup(r => r.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        var result = await _sut.CreateAsync(new CreateUserRequest("A@B.com", "Alice"));

        Assert.Equal("a@b.com", result.Email);
        Assert.Equal("Alice", result.DisplayName);
        Assert.False(result.IsLocked);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenEmailIsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateAsync(new CreateUserRequest("   ", "Alice")));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenEmailExists()
    {
        _repository.Setup(r => r.GetByEmailAsync("dup@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "dup@test.com" });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(new CreateUserRequest("dup@test.com", "Dup")));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenFound()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Email = "u@test.com",
            DisplayName = "U",
            CreatedAtUtc = DateTime.UtcNow
        };
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("u@test.com", result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.GetByIdAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Email = "a@test.com", DisplayName = "A", CreatedAtUtc = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Email = "b@test.com", DisplayName = "B", CreatedAtUtc = DateTime.UtcNow }
        };
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Email == "a@test.com");
        Assert.Contains(result, u => u.Email == "b@test.com");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedUser_WhenValid()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Email = "old@test.com",
            DisplayName = "Old",
            IsLocked = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _repository.Setup(r => r.GetByEmailAsync("new@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _repository.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.UpdateAsync(id, new UpdateUserRequest("New@Test.com", "  New Name  "));

        Assert.NotNull(result);
        Assert.Equal("new@test.com", result!.Email);
        Assert.Equal("New Name", result.DisplayName);
        Assert.NotNull(result.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenUserNotFound()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.UpdateAsync(id, new UpdateUserRequest("u@test.com", "Name"));

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenUserIsLocked()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = id, Email = "u@test.com", IsLocked = true });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(id, new UpdateUserRequest("u@test.com", "Name")));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenEmailUsedByAnotherUser()
    {
        var id = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var user = new User { Id = id, Email = "me@test.com", DisplayName = "Me", IsLocked = false };
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _repository.Setup(r => r.GetByEmailAsync("taken@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = otherId, Email = "taken@test.com" });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(id, new UpdateUserRequest("taken@test.com", "Me")));
    }

    [Fact]
    public async Task UpdateAsync_AllowsSameEmailForSameUser()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Email = "same@test.com",
            DisplayName = "Old",
            IsLocked = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _repository.Setup(r => r.GetByEmailAsync("same@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _repository.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.UpdateAsync(id, new UpdateUserRequest("same@test.com", "New"));

        Assert.NotNull(result);
        Assert.Equal("New", result!.DisplayName);
    }

    [Fact]
    public async Task LockAsync_SetsIsLocked()
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, Email = "u@test.com", DisplayName = "U", IsLocked = false };
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _repository.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.LockAsync(id);

        Assert.NotNull(result);
        Assert.True(result!.IsLocked);
    }

    [Fact]
    public async Task LockAsync_ReturnsNull_WhenUserNotFound()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.LockAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task UnlockAsync_ClearsIsLocked()
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, Email = "u@test.com", DisplayName = "U", IsLocked = true };
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _repository.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.UnlockAsync(id);

        Assert.NotNull(result);
        Assert.False(result!.IsLocked);
    }

    [Fact]
    public async Task UnlockAsync_ReturnsNull_WhenUserNotFound()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.UnlockAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var deleted = await _sut.DeleteAsync(id);

        Assert.True(deleted);
    }
}
