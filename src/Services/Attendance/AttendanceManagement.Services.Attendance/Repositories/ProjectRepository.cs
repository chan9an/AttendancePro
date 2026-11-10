using AttendanceManagement.Services.Attendance.Data;

namespace AttendanceManagement.Services.Attendance.Repositories;

/// <summary>
/// ProjectRepository extending ProjectAssignmentRepository to implement IProjectRepository.
/// </summary>
public class ProjectRepository : ProjectAssignmentRepository
{
    public ProjectRepository(AttendanceDbContext context) : base(context)
    {
    }
}
