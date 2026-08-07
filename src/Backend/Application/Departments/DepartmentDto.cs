namespace IAMS.Application.Departments;

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int UserCount);