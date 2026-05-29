using Microsoft.AspNetCore.Mvc;
using Moq;
using UserManagement.Api.Controllers;
using UserManagement.Core.DTOs;
using UserManagement.Core.Services;

namespace UserManagement.UnitTests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _service = new();
    private readonly UsersController _sut;

    public UsersControllerTests() => _sut = new UsersController(_service.Object);

    private static UserResponse SampleUser(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "u@test.com", "User", false, DateTime.UtcNow, null);

    [Fact]
    public async Task GetAll_ReturnsOkWithUsers()
    {
        var users = new List<UserResponse> { SampleUser(), SampleUser() };
        _service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(users, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var user = SampleUser();
        _service.Setup(s => s.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.GetById(user.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(user, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponse?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreated()
    {
        var user = SampleUser();
        _service.Setup(s => s.CreateAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.Create(new CreateUserRequest("u@test.com", "User"), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(UsersController.GetById), created.ActionName);
        Assert.Equal(user, created.Value);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenEmailExists()
    {
        _service.Setup(s => s.CreateAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("duplicate"));

        var result = await _sut.Create(new CreateUserRequest("dup@test.com", "Dup"), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.NotNull(conflict.Value);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenEmailInvalid()
    {
        _service.Setup(s => s.CreateAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Email is required."));

        var result = await _sut.Create(new CreateUserRequest("", "User"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenUpdated()
    {
        var user = SampleUser();
        _service.Setup(s => s.UpdateAsync(user.Id, It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.Update(user.Id, new UpdateUserRequest("u@test.com", "User"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(user, ok.Value);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenUserMissing()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.UpdateAsync(id, It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponse?)null);

        var result = await _sut.Update(id, new UpdateUserRequest("u@test.com", "User"), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_ReturnsConflict_WhenLocked()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.UpdateAsync(id, It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("locked"));

        var result = await _sut.Update(id, new UpdateUserRequest("u@test.com", "User"), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Lock_ReturnsOk_WhenFound()
    {
        var user = SampleUser() with { IsLocked = true };
        _service.Setup(s => s.LockAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.Lock(user.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(user, ok.Value);
    }

    [Fact]
    public async Task Lock_ReturnsNotFound_WhenMissing()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.LockAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponse?)null);

        var result = await _sut.Lock(id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Unlock_ReturnsOk_WhenFound()
    {
        var user = SampleUser();
        _service.Setup(s => s.UnlockAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.Unlock(user.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(user, ok.Value);
    }

    [Fact]
    public async Task Unlock_ReturnsNotFound_WhenMissing()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.UnlockAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponse?)null);

        var result = await _sut.Unlock(id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenDeleted()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenMissing()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _sut.Delete(id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
