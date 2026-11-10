namespace AttendanceService.DTOs;

public record CreateProjectDto(
    string Name,
    string? Code,
    string? Description);

public record UpdateProjectDto(
    string Name,
    string? Code,
    string? Description,
    bool IsActive);

public record ProjectResponseDto(
    Guid Id,
    string Name,
    string? Code,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public record AssignEmployeeProjectDto(
    Guid EmployeeId,
    Guid ProjectId,
    DateOnly StartDate,
    DateOnly? EndDate);

public record ProjectAssignmentResponseDto(
    Guid Id,
    Guid EmployeeId,
    Guid ProjectId,
    string ProjectName,
    bool IsActive,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateTime AssignedAtUtc);
