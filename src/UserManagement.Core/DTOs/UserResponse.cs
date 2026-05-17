namespace UserManagement.Core.DTOs;

public record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsLocked,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
