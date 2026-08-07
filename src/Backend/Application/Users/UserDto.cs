namespace IAMS.Application.Users;

public sealed record RoleDto(Guid Id, string Name, string? Description);

public sealed record UserDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    bool IsActive,
    bool MustChangePassword,
    Guid? DepartmentId,
    string? DepartmentName,
    IReadOnlyList<RoleDto> Roles);