using UserManagement.Core.DTOs;
using UserManagement.Core.Entities;
using UserManagement.Core.Interfaces;

namespace UserManagement.Core.Services;

public class UserService(IUserRepository repository) : IUserService
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(request));
        }

        if (await repository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new InvalidOperationException($"User with email '{email}' already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            IsLocked = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await repository.AddAsync(user, cancellationToken);
        return ToResponse(user);
    }

    public async Task<UserResponse?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (user.IsLocked)
        {
            throw new InvalidOperationException("Cannot update a locked user. Unlock first.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await repository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null && existing.Id != id)
        {
            throw new InvalidOperationException($"Email '{email}' is already in use.");
        }

        user.Email = email;
        user.DisplayName = request.DisplayName.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;

        await repository.UpdateAsync(user, cancellationToken);
        return ToResponse(user);
    }

    public async Task<UserResponse?> LockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.IsLocked = true;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await repository.UpdateAsync(user, cancellationToken);
        return ToResponse(user);
    }

    public async Task<UserResponse?> UnlockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.IsLocked = false;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await repository.UpdateAsync(user, cancellationToken);
        return ToResponse(user);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => repository.DeleteAsync(id, cancellationToken);

    public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : ToResponse(user);
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await repository.GetAllAsync(cancellationToken);
        return users.Select(ToResponse).ToList();
    }

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.IsLocked, user.CreatedAtUtc, user.UpdatedAtUtc);
}
