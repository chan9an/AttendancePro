using AttendanceManagement.Services.Attendance.Application.DTOs.Attendance;
using AttendanceManagement.Services.Attendance.Domain.Entities;

namespace AttendanceManagement.Services.Attendance.Application.Mappings;

/// <summary>
/// Static extension methods providing reflection-free compile-time mapping for Attendance entities.
/// </summary>
public static class AttendanceMappingExtensions
{
    public static AttendanceResponseDto ToResponseDto(this AttendanceRecord record)
    {
        return new AttendanceResponseDto(
            Id: record.Id,
            EmployeeId: record.EmployeeId,
            Date: record.Date,
            ProjectId: record.ProjectId,
            ProjectName: record.ProjectName,
            Remarks: record.Remarks,
            Status: record.Status.ToString(),
            CreatedAtUtc: record.CreatedAtUtc
        );
    }

    public static IReadOnlyList<AttendanceResponseDto> ToResponseDtoList(this IEnumerable<AttendanceRecord> records)
    {
        return records.Select(ToResponseDto).ToList();
    }
}
