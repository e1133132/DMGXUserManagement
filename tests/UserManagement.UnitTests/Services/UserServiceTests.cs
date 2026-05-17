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
    public async Task CreateAsync_Throws_WhenEmailExists()
    {
        _repository.Setup(r => r.GetByEmailAsync("dup@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "dup@test.com" });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(new CreateUserRequest("dup@test.com", "Dup")));
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
    public async Task DeleteAsync_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var deleted = await _sut.DeleteAsync(id);

        Assert.True(deleted);
    }
}
